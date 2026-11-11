using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace frmwpsoffice
{
    /// <summary>
    /// Self-update: download the release exe, verify it against the release's SHA256SUMS.txt, then let the
    /// downloaded copy replace the installed exe once the running app has exited.
    /// </summary>
    internal static class Updater
    {
        public const string ApplyArg = "--apply-update";
        public const string ElevatedArg = "--elevated";
        public const string SumsFileName = "SHA256SUMS.txt";

        private const string TempPrefix = "WPSUninstallerUpdate_";
        private const string AllowedPrefix = "https://github.com/ThanhNguyenVN93/WPS-office-uninstaller-tool/releases/download/";

        /// <summary>Only files of this project's GitHub releases are ever downloaded.</summary>
        public static bool IsAllowedUrl(string url)
        {
            return url != null && url.StartsWith(AllowedPrefix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Reads "&lt;64 hex&gt;  &lt;file name&gt;" lines (sha256sum format) and returns the hash for fileName.</summary>
        public static string ParseExpectedSha256(string sumsText, string fileName)
        {
            if (string.IsNullOrEmpty(sumsText) || string.IsNullOrEmpty(fileName)) return null;
            foreach (string raw in sumsText.Split('\n'))
            {
                Match m = Regex.Match(raw.Trim(), @"^([0-9a-fA-F]{64})\s+\*?(.+)$");
                if (m.Success && string.Equals(m.Groups[2].Value.Trim(), fileName, StringComparison.OrdinalIgnoreCase))
                    return m.Groups[1].Value.ToLowerInvariant();
            }
            return null;
        }

        public static string Sha256Of(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(fs).Select(b => b.ToString("x2")));
        }

        /// <summary>Downloads and verifies the new exe. Returns the path of the verified copy; throws on any problem.</summary>
        public static async Task<string> DownloadAndVerifyAsync(UpdateInfo info, Action<int> progress)
        {
            if (info.ExeUrl == null || info.SumsUrl == null)
                throw new InvalidOperationException("This release has no checksum file, so the download cannot be verified.");
            if (!IsAllowedUrl(info.ExeUrl) || !IsAllowedUrl(info.SumsUrl))
                throw new InvalidOperationException("Unexpected download address.");

            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            string dir = Path.Combine(Path.GetTempPath(), TempPrefix + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, "WPS OFFICE UNINSTALLER TOOL.exe");

            try
            {
                string expected;
                using (var wc = new WebClient())
                {
                    wc.Headers[HttpRequestHeader.UserAgent] = "WPS-Uninstaller-Tool";
                    string sums = await wc.DownloadStringTaskAsync(info.SumsUrl);
                    expected = ParseExpectedSha256(sums, info.ExeName);
                    if (expected == null)
                        throw new InvalidOperationException("The checksum file has no entry for " + info.ExeName + ".");

                    wc.DownloadProgressChanged += (s, e) => { if (progress != null) progress(e.ProgressPercentage); };
                    await wc.DownloadFileTaskAsync(info.ExeUrl, file);
                }

                string actual = Sha256Of(file);
                if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The downloaded file does not match its checksum and was discarded.");

                // The file must really be the announced version (not an older or different build).
                Version fileVersion;
                string fv = FileVersionInfo.GetVersionInfo(file).FileVersion;
                if (!Version.TryParse(fv, out fileVersion) ||
                    UpdateChecker.Normalize(fileVersion).CompareTo(UpdateChecker.Normalize(info.Latest)) != 0)
                    throw new InvalidOperationException("The downloaded file is not version " + info.Latest + ".");

                return file;
            }
            catch
            {
                try { Directory.Delete(dir, true); } catch { }
                throw;
            }
        }

        /// <summary>Starts the verified copy in "apply" mode; the caller must exit right afterwards.</summary>
        public static void StartApply(string stagedExe)
        {
            string self = Application.ExecutablePath;
            var psi = new ProcessStartInfo(stagedExe)
            {
                Arguments = ApplyArg + " " + Process.GetCurrentProcess().Id + " \"" + self + "\"",
                UseShellExecute = false
            };
            Process.Start(psi);
        }

        /// <summary>
        /// Runs in the downloaded copy: waits for the old app to exit, overwrites it with this file and starts it again.
        /// </summary>
        public static void RunApply(string pidText, string destination, bool elevated)
        {
            string source = Application.ExecutablePath;
            Logger.Info("Applying update: " + source + " -> " + destination + (elevated ? " (elevated)" : ""));
            try
            {
                int pid;
                if (int.TryParse(pidText, out pid))
                {
                    try { using (Process old = Process.GetProcessById(pid)) old.WaitForExit(30000); }
                    catch (ArgumentException) { /* already gone */ }
                }

                Exception last = null;
                for (int i = 0; i < 20; i++)
                {
                    try
                    {
                        File.Copy(source, destination, true);
                        last = null;
                        break;
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        last = ex;
                        if (!elevated)
                        {
                            // The install folder needs administrator rights: run this same step elevated.
                            Process.Start(new ProcessStartInfo(source)
                            {
                                Arguments = ApplyArg + " " + pidText + " \"" + destination + "\" " + ElevatedArg,
                                UseShellExecute = true,
                                Verb = "runas"
                            });
                            return;
                        }
                        break;
                    }
                    catch (IOException ex)
                    {
                        last = ex; // old exe still being released
                        Thread.Sleep(1000);
                    }
                }

                if (last != null) throw last;

                Process.Start(new ProcessStartInfo(destination) { UseShellExecute = true });
                Logger.Info("Update applied, app restarted");
            }
            catch (Exception ex)
            {
                Logger.Error("Applying the update failed", ex);
                MessageBox.Show(
                    "The update could not be installed: " + ex.Message + Environment.NewLine + Environment.NewLine +
                    "Please download the new version from the Releases page and replace the file manually.",
                    "Update failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>Best effort: removes leftover download folders from earlier updates.</summary>
        public static void CleanupOldStaging()
        {
            try
            {
                string running = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
                foreach (string dir in Directory.GetDirectories(Path.GetTempPath(), TempPrefix + "*"))
                {
                    if (string.Equals(dir.TrimEnd('\\'), running.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) continue;
                    try { Directory.Delete(dir, true); } catch { }
                }
            }
            catch { }
        }
    }
}
