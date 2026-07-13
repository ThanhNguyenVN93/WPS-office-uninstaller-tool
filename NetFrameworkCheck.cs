using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Microsoft.Win32;

namespace frmwpsoffice
{
    internal static class NetFrameworkCheck
    {
        private const int Net48ReleaseKey = 528040;

        // Official Microsoft permalink for the .NET Framework 4.8 offline installer.
        private const string OfflineInstallerUrl = "https://go.microsoft.com/fwlink/?LinkId=2088631";

        public static bool IsNet48OrLaterInstalled()
        {
            try
            {
                RegistryView view = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                using (RegistryKey ndpKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\"))
                {
                    return ndpKey?.GetValue("Release") is int release && release >= Net48ReleaseKey;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if the app can continue starting (4.8 already present).
        /// Returns false if 4.8 was missing (whether or not install succeeded) so the caller can exit and let the user relaunch.
        /// </summary>
        public static bool EnsureInstalled()
        {
            if (IsNet48OrLaterInstalled()) return true;

            DialogResult confirm = MessageBox.Show(
                ".NET Framework 4.8 is required but was not found on this computer.\n\nDownload and install it now? (Administrator rights required)",
                ".NET Framework 4.8 Required",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return false;

            string installerPath = Path.Combine(Path.GetTempPath(), "NDP48-x86-x64-AllOS-ENU.exe");

            try
            {
                // Machines missing .NET 4.8 are often old enough that TLS 1.2 isn't the default;
                // Microsoft's download servers require it, so force it before connecting.
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(OfflineInstallerUrl, installerPath);
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = "/q /norestart",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                using (Process installProcess = Process.Start(startInfo))
                {
                    installProcess.WaitForExit();
                }

                MessageBox.Show(
                    ".NET Framework 4.8 has been installed.\n\nPlease restart the application. A system restart may also be required.",
                    "Installation Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to download or install .NET Framework 4.8: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                try { if (File.Exists(installerPath)) File.Delete(installerPath); } catch { }
            }

            return false;
        }
    }
}
