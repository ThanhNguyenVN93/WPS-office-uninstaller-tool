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
            catch { return null; }
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
                                // Skip if no permission
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
                { txtcheck.Text = $"Access permission Roaming error: Need run Admin permission. ({authEx.Message})"; return false; }
                catch (Exception ex)
                { txtcheck.Text = $"Error when deleting Roaming data: {ex.Message}"; return false; }
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

        private void linkupdate_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            _ = CheckForUpdateAsync(true);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!DonateSettings.DontShowAgain)
            {
                using (frmDonate donateForm = new frmDonate())
                {
                    donateForm.ShowDialog(this);
                }
            }
        }
        private void btnuninstall_Click(object sender, EventArgs e)
        {
            string wpsRootPath = txtdirect.Text;
            string uninstallerPath = null;

            if (string.IsNullOrEmpty(wpsRootPath) || !Directory.Exists(wpsRootPath))
            {
                txtcheck.Text = "ERROR: Cannot find WPS Office folder.";
                return;
            }

            KillWPSProcess();
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

        private void CleanWPSResidualFiles()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string kingsoftLocalPath = Path.Combine(appdata, "Kingsoft");

            if (!Directory.Exists(kingsoftLocalPath))
            {
                txtcheck.Text = "Kingsoft folder not found (already clean).";
                return;
            }

            KillWPSProcess();
            System.Threading.Thread.Sleep(300);

            const int MaxRetries = 3;
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    Directory.Delete(kingsoftLocalPath, true);
                    txtcheck.Text = "Local data deleted successfully.";
                    return;
                }
                catch (IOException)
                {
                    if (attempt < MaxRetries)
                    {
                        txtcheck.Text = $"Retrying ({attempt}/{MaxRetries}): file is locked...";
                        System.Threading.Thread.Sleep(800);
                    }
                    else
                    {
                        txtcheck.Text = $"I/O Error: Unable to delete. File may be locked. Request admin rights for cleanup button.";
                        return;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    txtcheck.Text = "Access denied. Will need admin rights for full cleanup.";
                    return;
                }
                catch (Exception ex)
                {
                    txtcheck.Text = $"Error: {ex.Message}";
                    return;
                }
            }
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
                CleanWPSResidualFiles();

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

                string blockMsg = blockCode == 0 ? "OK - WPS is blocked (log out/in to fully apply)"
                    : blockCode == -2 ? "Skipped - administrator permission was denied"
                    : "FAILED - some block entries could not be written";
                string gpMsg = gpCode == 0 ? "OK - gpedit.msc is available"
                    : gpCode == 2 ? "Restart the computer, then check gpedit.msc again"
                    : gpCode == -2 ? "Skipped - administrator permission was denied"
                    : "FAILED - this Windows edition may not support gpedit.msc (blocking still works)";

                bool allOk = blockCode == 0 && gpCode == 0;
                txtcheck.Text = allOk ? "All cleanup completed!" : "Cleanup completed with warnings.";
                MessageBox.Show(
                    "Local data cleanup: OK\nWPS block: " + blockMsg + "\nGroup Policy Editor: " + gpMsg,
                    allOk ? "Success" : "Completed with warnings",
                    MessageBoxButtons.OK,
                    allOk ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                Application.Exit();
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
                return -2; // user cancelled UAC
            }
            catch (Exception ex)
            {
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
