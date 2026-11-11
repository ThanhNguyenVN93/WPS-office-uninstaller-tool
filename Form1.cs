using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace frmwpsoffice
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private string FindFileRecursively(string rootPath, string fileName)
        {
            if (!Directory.Exists(rootPath)) return null;
            try
            {
                return Directory.GetFiles(rootPath, fileName, SearchOption.AllDirectories).FirstOrDefault();
            }
            catch (Exception ex) { Logger.Error("Search failed in " + rootPath, ex); return null; }
        }

        // -------------------------------------------------------------
        // LOGIC KIỂM TRA WPS OFFICE
        // -------------------------------------------------------------

        public void CheckWPSInstallLocation()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string wpsAppdataRoot = Path.Combine(appdata, "Kingsoft", "WPS Office");

            if (FindFileRecursively(wpsAppdataRoot, "ksolaunch.exe") != null)
            {
                txtcheck.Text = "WPS Office exists in AppData.";
                txtdirect.Text = wpsAppdataRoot;
                return;
            }

            string programfiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programfilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            string wpsProgramFilesRoot = Path.Combine(programfiles, "WPS Office");
            if (File.Exists(Path.Combine(wpsProgramFilesRoot, "ksolaunch.exe")))
            {
                txtcheck.Text = "WPS Office exists in Program Files.";
                txtdirect.Text = wpsProgramFilesRoot;
                return;
            }

            string wpsProgramFilesX86Root = Path.Combine(programfilesx86, "WPS Office");
            if (File.Exists(Path.Combine(wpsProgramFilesX86Root, "ksolaunch.exe")))
            {
                txtcheck.Text = "WPS Office exists in Program Files (x86).";
                txtdirect.Text = wpsProgramFilesX86Root;
                return;
            }

            txtcheck.Text = "WPS Office not found. Move to Clean Up Kingsoft residual data Mode.";
            
            txtdirect.Text = Path.Combine(appdata, "Kingsoft");
            btnuninstall.Enabled = false;
        }
        private void KillWPSProcess()
        {
            const string targetName = "wps";
            int killedCount = 0;
            try
            {
                var wpsProcesses = Process.GetProcesses()
                                          .Where(p => p.ProcessName.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                                          .ToList();

                if (wpsProcesses.Count > 0)
                {
                    foreach (Process p in wpsProcesses)
                    {
                        if (!p.HasExited)
                        {
                            try
                            {
                                p.Kill();
                                killedCount++;
                            }
                            catch
                            {
                                Logger.Warn("Cannot kill process id " + p.Id + " (no permission?)");
                            }
                        }
                    }
                    txtcheck.Text = killedCount > 0 
                        ? $"{killedCount} WPS process(es) terminated." 
                        : "No WPS processes found running.";
                }
                else
                {
                    txtcheck.Text = "No WPS processes found running.";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("KillWPSProcess failed", ex);
                txtcheck.Text = $"Warning: {ex.Message}";
            }
        }
        private bool CleanWPSRoamingData()
        {
            string appdataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string kingsoftRoamingPath = Path.Combine(appdataRoaming, "kingsoft");

            if (Directory.Exists(kingsoftRoamingPath))
            {
                try
                { Directory.Delete(kingsoftRoamingPath, true); return true; }
                catch (UnauthorizedAccessException authEx)
                { Logger.Error("Roaming cleanup: access denied", authEx); txtcheck.Text = $"Access permission Roaming error: Need run Admin permission. ({authEx.Message})"; return false; }
                catch (Exception ex)
                { Logger.Error("Roaming cleanup failed", ex); txtcheck.Text = $"Error when deleting Roaming data: {ex.Message}"; return false; }
            }
            return true;
        }
        // FORM EVENTS
        // -------------------------------------------------------------
        private void Form1_Load(object sender, EventArgs e)
        {
            string r = "";
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
            {
                ManagementObjectCollection information = searcher.Get();
                if (information != null)
                {
                    foreach (ManagementObject obj in information)
                    {
                        r = obj["Caption"].ToString() + " - " + obj["OSArchitecture"].ToString();
                    }
                }
                r = r.Replace("NT 5.1.2600", "XP");
                r = r.Replace("NT 5.2.3790", "Server 2003");
                lblsys.Text = r;
            }

            CheckWPSInstallLocation();
            Logger.Info("Scan: " + txtcheck.Text + " | dir=" + txtdirect.Text);

            linkupdate.Text = $"v{UpdateChecker.CurrentVersion.ToString(3)} - Check for updates";
            _ = CheckForUpdateAsync(false);

            string currentUser = Environment.UserName;
            lblsys.Text += $"\nCurrent User: {currentUser}";
        }
        private async Task CheckForUpdateAsync(bool userInitiated)
        {
            linkupdate.Enabled = false;
            try
            {
                UpdateInfo info = await UpdateChecker.CheckAsync();
                if (IsDisposed) return;

                if (info == null)
                {
                    if (userInitiated)
                        MessageBox.Show("Could not check for updates. Please check your internet connection.", "Update", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (info.IsNewer)
                {
                    Logger.Info("Update available: " + info.Tag);
                    linkupdate.Text = $"New version {info.Tag} available!";
                    DialogResult r = MessageBox.Show(
                        $"A new version is available: {info.Tag} (current: v{UpdateChecker.CurrentVersion.ToString(3)}).\n\nOpen the download page?",
                        "Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (r == DialogResult.Yes)
                        UpdateChecker.OpenInBrowser(info.PageUrl);
                }
                else if (userInitiated)
                {
                    MessageBox.Show("You are using the latest version.", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            finally
            {
                if (!IsDisposed) linkupdate.Enabled = true;
            }
        }

        private void linklog_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Logger.OpenFolder();
        }

        private void linkupdate_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            _ = CheckForUpdateAsync(true);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!DonateSettings.DontShowAgain)
            {
                Logger.Info("Closing (" + e.CloseReason + "): showing donate dialog");
                if (NTDonate.DonateForm.IsWebView2Usable() && NTDonate.DonateForm.IsSiteReachable())
                {
                    try
                    {
                        using (NTDonate.DonateForm web = new NTDonate.DonateForm(false))
                        {
                            web.ShowDialog(this);
                            if (web.DontShowAgain) DonateSettings.DontShowAgain = true;
                        }
                        return;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Web donate form failed, using offline form", ex);
                    }
                }

                // No WebView2 / offline / site unreachable: local QR form
                using (frmDonate donateForm = new frmDonate())
                {
                    donateForm.ShowDialog(this);
                }
            }
        }
        /// <summary>True when a block created by blockwps.bat is currently in the registry (readable without admin).</summary>
        private static bool IsBlockActive()
        {
            try
            {
                using (var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\DisallowRun"))
                {
                    if (k != null && (k.GetValue("wps.exe") != null || k.GetValue("wps_office_inst.exe") != null))
                        return true;
                }
                using (var hk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Safer\CodeIdentifiers\0\Paths\{7a1d0c00-5a11-4f00-9a00-000000000001}"))
                {
                    if (hk != null) return true;
                }
                foreach (var view in new[] { Microsoft.Win32.RegistryView.Registry64, Microsoft.Win32.RegistryView.Registry32 })
                using (var hklm = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, view))
                using (var k = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\wps.exe"))
                {
                    if (k != null && k.GetValue("Debugger") != null) return true;
                }
            }
            catch (Exception ex) { Logger.Warn("IsBlockActive failed: " + ex.Message); }
            return false;
        }

        private async void btnuninstall_Click(object sender, EventArgs e)
        {
            string wpsRootPath = txtdirect.Text;
            string uninstallerPath = null;

            if (string.IsNullOrEmpty(wpsRootPath) || !Directory.Exists(wpsRootPath))
            {
                txtcheck.Text = "ERROR: Cannot find WPS Office folder.";
                return;
            }

            KillWPSProcess();
            // A previous CLEAN UP leaves the WPS block active; the WPS uninstaller launches WPS components
            // itself, so lift the block first (CLEAN UP re-applies it afterwards).
            if (IsBlockActive())
            {
                txtcheck.Text = "Lifting WPS block before uninstall...";
                btnuninstall.Enabled = false;
                string sid0 = System.Security.Principal.WindowsIdentity.GetCurrent().User?.Value ?? "";
                int rc = await RunEmbeddedBatchFileAsync("frmwpsoffice.blockwps.bat", $"/silent /remove {sid0}");
                Logger.Info("Lifted WPS block before uninstall: rc=" + rc);
                if (IsDisposed) return;
                btnuninstall.Enabled = true;
                if (rc != 0)
                {
                    txtcheck.Text = "Could not lift the WPS block (admin permission needed). Uninstall may be restricted.";
                    return;
                }
            }

            uninstallerPath = FindFileRecursively(wpsRootPath, "uninst.exe");

            if (uninstallerPath != null)
            {
                txtcheck.Text = $"Found Uninstaller: {uninstallerPath}";
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = uninstallerPath,
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    Process.Start(startInfo);
                    Logger.Info("Started uninstaller: " + uninstallerPath);
                    txtcheck.Text = "Running WPS Office Uninstaller (admin mode)...";
                }
                catch (Exception ex)
                {
                    txtcheck.Text = $"Error: {ex.Message}";
                }
            }
            else
            {
                txtcheck.Text = "Error: Cannot find uninst.exe in installation folder.";
            }
        }

        /// <summary>Kills this session's explorer.exe, deletes the folder right away (before the shell reloads its extensions), then makes sure the shell is back.</summary>
        private static bool RestartExplorerAndDelete(string path)
        {
            int session = Process.GetCurrentProcess().SessionId;
            Logger.Info("Restarting Explorer to release locked WPS files");
            foreach (Process p in Process.GetProcessesByName("explorer").Where(x => x.SessionId == session).ToList())
            {
                try { p.Kill(); p.WaitForExit(3000); }
                catch (Exception ex) { Logger.Warn("Cannot stop explorer.exe: " + ex.Message); }
            }

            int failed = DeleteTreeForce(path);
            bool ok = failed == 0 && !Directory.Exists(path);
            Logger.Info("Delete after Explorer restart: " + (ok ? "OK" : failed + " item(s) left"));

            // Windows normally respawns the shell by itself; start it only if that did not happen.
            for (int i = 0; i < 10; i++)
            {
                if (Process.GetProcessesByName("explorer").Any(x => x.SessionId == session)) return ok;
                System.Threading.Thread.Sleep(500);
            }
            try { Process.Start("explorer.exe"); } catch (Exception ex) { Logger.Warn("Cannot start explorer.exe: " + ex.Message); }
            return ok;
        }

        /// <summary>Deletes a directory tree best-effort (clears read-only flags). Returns the number of items that could not be removed.</summary>
        private static int DeleteTreeForce(string dir)
        {
            int failed = 0;
            try
            {
                foreach (string file in Directory.GetFiles(dir))
                {
                    try { File.SetAttributes(file, FileAttributes.Normal); File.Delete(file); }
                    catch (Exception ex) { failed++; Logger.Warn("Cannot delete file " + file + ": " + ex.Message); }
                }
                foreach (string sub in Directory.GetDirectories(dir))
                {
                    // Do not follow junctions/symlinks, just drop the link itself.
                    if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) != 0)
                    {
                        try { Directory.Delete(sub, false); }
                        catch (Exception ex) { failed++; Logger.Warn("Cannot remove link " + sub + ": " + ex.Message); }
                        continue;
                    }
                    failed += DeleteTreeForce(sub);
                }
                File.SetAttributes(dir, FileAttributes.Normal);
                Directory.Delete(dir, false);
            }
            catch (Exception ex)
            {
                failed++;
                Logger.Warn("Cannot delete folder " + dir + ": " + ex.Message);
            }
            return failed;
        }

        /// <summary>Returns true when the local Kingsoft data is gone afterwards.</summary>
        private bool CleanWPSResidualFiles()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string kingsoftLocalPath = Path.Combine(appdata, "Kingsoft");

            if (!Directory.Exists(kingsoftLocalPath))
            {
                txtcheck.Text = "Kingsoft folder not found (already clean).";
                return true;
            }

            KillWPSProcess();
            // The WPS uninstaller may still be running and holding files; stop it too.
            foreach (Process p in Process.GetProcesses().Where(x => x.ProcessName.Equals("uninst", StringComparison.OrdinalIgnoreCase)).ToList())
            {
                try
                {
                    string exePath = p.MainModule?.FileName ?? "";
                    if (exePath.StartsWith(kingsoftLocalPath, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Info("Stopping WPS uninstaller still running: " + exePath);
                        p.Kill();
                        p.WaitForExit(2000);
                    }
                }
                catch { }
            }
            System.Threading.Thread.Sleep(300);

            const int MaxRetries = 3;
            int failed = 0;
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                failed = DeleteTreeForce(kingsoftLocalPath);
                if (failed == 0 && !Directory.Exists(kingsoftLocalPath))
                {
                    Logger.Info("Local Kingsoft data deleted: " + kingsoftLocalPath);
                    txtcheck.Text = "Local data deleted successfully.";
                    return true;
                }

                Logger.Warn("Delete Kingsoft local data attempt " + attempt + "/" + MaxRetries + ": " + failed + " item(s) left");
                if (attempt < MaxRetries)
                {
                    txtcheck.Text = $"Retrying ({attempt}/{MaxRetries}): some files are locked...";
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(800);
                }
            }

            // Typical leftover: WPS shell-extension DLLs (qingshellext64.dll_d_*, kbaseconfigcenter64.dll_d_*) mapped into explorer.exe.
            if (Directory.Exists(kingsoftLocalPath))
            {
                DialogResult dr = MessageBox.Show(
                    "Some WPS files are locked by Windows Explorer (WPS shell extension). Restart Windows Explorer to release them? The taskbar will disappear for a few seconds.",
                    "Files in use", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr == DialogResult.Yes && RestartExplorerAndDelete(kingsoftLocalPath))
                {
                    txtcheck.Text = "Local data deleted successfully (Explorer restarted).";
                    return true;
                }
            }

            txtcheck.Text = "Some local data could not be deleted (files locked or access denied). See log.";
            return false;
        }
        private async void btncleanup_Click_1(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtdirect.Text))
            {
                txtcheck.Text = "Please scan first to detect WPS Office location.";
                return;
            }

            DialogResult result = MessageBox.Show(
                "This will delete all WPS Office data for current user.\n\nThis operation may require Administrator privileges.\n\nContinue?",
                "Confirm Cleanup",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            btncleanup.Enabled = false;
            btnuninstall.Enabled = false;
            try
            {
                bool localSuccess = CleanWPSResidualFiles();

                bool roamingSuccess = CleanWPSRoamingData();
                if (!roamingSuccess)
                {
                    txtcheck.Text += "\nWarning: Some data could not be cleaned.";
                    return;
                }

                txtcheck.Text = "Cleanup done. Applying WPS block...";

                // Pass the real user's SID so the per-user policy is written to the right hive even if UAC elevates another admin account.
                string sid = System.Security.Principal.WindowsIdentity.GetCurrent().User?.Value ?? "";
                int blockCode = await RunEmbeddedBatchFileAsync("frmwpsoffice.blockwps.bat", $"/silent {sid}");

                txtcheck.Text = "Enabling Group Policy Editor...";
                // Make sure the Local Group Policy Editor exists (Windows Home) so the block policy can be reviewed/managed.
                int gpCode = await RunEmbeddedBatchFileAsync("frmwpsoffice.enablegpedit.bat", "/silent");
                Logger.Info("Scripts finished: blockwps=" + blockCode + " enablegpedit=" + gpCode);

                string blockMsg = blockCode == 0 ? "OK - WPS is blocked (restart the computer to fully apply)"
                    : blockCode == -2 ? "Skipped - administrator permission was denied"
                    : "FAILED - some block entries could not be written";
                string gpMsg = gpCode == 0 ? "OK - gpedit.msc is available"
                    : gpCode == 2 ? "Restart the computer, then check gpedit.msc again"
                    : gpCode == -2 ? "Skipped - administrator permission was denied"
                    : "FAILED - this Windows edition may not support gpedit.msc (blocking still works)";

                bool allOk = localSuccess && blockCode == 0 && gpCode == 0;
                txtcheck.Text = allOk ? "All cleanup completed!" : "Cleanup completed with warnings.";
                MessageBox.Show(
                    "Local data cleanup: " + (localSuccess ? "OK" : "INCOMPLETE - some files were locked or access was denied (see log)") + "\nWPS block: " + blockMsg + "\nGroup Policy Editor: " + gpMsg,
                    allOk ? "Success" : "Completed with warnings",
                    MessageBoxButtons.OK,
                    allOk ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                Close();
            }
            finally
            {
                if (!IsDisposed)
                {
                    btncleanup.Enabled = true;
                    btnuninstall.Enabled = true;
                }
            }
        }

        /// <summary>
        /// Extracts an embedded batch file to a unique temp file, runs it elevated and waits for it.
        /// Returns the script exit code, -1 on internal error, -2 when the UAC prompt was declined.
        /// </summary>
        public static async Task<int> RunEmbeddedBatchFileAsync(string resourceName, string arguments = "")
        {
            string tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".bat");
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        MessageBox.Show($"Embedded resource not found: {resourceName}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return -1;
                    }
                    using (FileStream fileStream = File.Create(tempFilePath))
                    {
                        stream.CopyTo(fileStream);
                    }
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = tempFilePath,
                    Arguments = arguments,
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                return await Task.Run(() =>
                {
                    using (Process p = Process.Start(startInfo))
                    {
                        if (p == null) return -1;
                        p.WaitForExit();
                        return p.ExitCode;
                    }
                });
            }
            catch (Win32Exception wex) when (wex.NativeErrorCode == 1223)
            {
                Logger.Warn("UAC declined for " + resourceName);
                return -2; // user cancelled UAC
            }
            catch (Exception ex)
            {
                Logger.Error("Error running " + resourceName, ex);
                MessageBox.Show("Error running batch file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
            finally
            {
                try { File.Delete(tempFilePath); } catch { }
            }
        }
    }
}
