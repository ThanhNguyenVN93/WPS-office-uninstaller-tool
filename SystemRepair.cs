using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace frmwpsoffice
{
    internal sealed class SystemRepairResult
    {
        /// <summary>False when the UAC prompt was declined (only the per-user part ran).</summary>
        public bool MachinePartRan;
        public int Changed;
        public int Failed;
        public int OfficeTypes;
        public List<string> StillBroken = new List<string>();
    }

    /// <summary>
    /// Runs the file-association restore + leftover cleanup: the machine part in an elevated copy of this exe
    /// (one UAC prompt), the per-user part in this process so it always edits the real user's HKCU.
    /// </summary>
    internal static class SystemRepair
    {
        public const string ElevatedArg = "--wps-repair";

        /// <summary>Entry point of the elevated copy: <c>exe --wps-repair "&lt;report file&gt;"</c>.</summary>
        public static int RunElevatedChild(string reportPath)
        {
            var report = new RepairReport();
            try { FileAssocRestorer.RestoreMachine(report); }
            catch (Exception ex) { report.Fail("File association (machine) failed: " + ex.Message); }
            try { WpsLeftoverCleaner.CleanMachine(report); }
            catch (Exception ex) { report.Fail("Leftover cleanup (machine) failed: " + ex.Message); }
            try { report.WriteTo(reportPath); } catch { return 3; }
            return report.Failed == 0 ? 0 : 1;
        }

        public static async Task<SystemRepairResult> RunAsync()
        {
            var result = new SystemRepairResult();
            var total = new RepairReport();

            string reportPath = Path.Combine(Path.GetTempPath(), "wpsrepair-" + Guid.NewGuid().ToString("N") + ".txt");
            int rc = await Task.Run(() =>
            {
                try
                {
                    using (Process p = Process.Start(new ProcessStartInfo
                    {
                        FileName = Application.ExecutablePath,
                        Arguments = ElevatedArg + " \"" + reportPath + "\"",
                        UseShellExecute = true,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden
                    }))
                    {
                        if (p == null) return -1;
                        p.WaitForExit();
                        return p.ExitCode;
                    }
                }
                catch (Win32Exception wex) when (wex.NativeErrorCode == 1223) { return -2; }
                catch (Exception ex) { Logger.Error("Elevated repair could not start", ex); return -1; }
            });
            Logger.Info("Elevated repair finished: rc=" + rc);

            RepairReport machine = RepairReport.ReadFrom(reportPath);
            try { File.Delete(reportPath); } catch { }
            result.MachinePartRan = machine != null;
            if (machine != null) machine.FlushToLog("  [admin] ");
            total.Merge(machine);

            // Per-user part (no admin). If the machine part did not run, it also points types to Office per user.
            var user = new RepairReport();
            await Task.Run(() =>
            {
                try { FileAssocRestorer.RestoreUser(user, result.MachinePartRan); }
                catch (Exception ex) { user.Fail("File association (user) failed: " + ex.Message); }
                try { WpsLeftoverCleaner.CleanUser(user); }
                catch (Exception ex) { user.Fail("Leftover cleanup (user) failed: " + ex.Message); }
                FileAssocRestorer.RefreshShell();
            });
            user.FlushToLog("  [user] ");
            total.Merge(user);

            result.Changed = total.Changed;
            result.Failed = total.Failed;
            result.StillBroken = FileAssocRestorer.StillBroken();
            result.OfficeTypes = FileAssocRestorer.CountOnOffice();
            Logger.Info("Repair: changed=" + result.Changed + " failed=" + result.Failed + " officeTypes=" + result.OfficeTypes +
                        " stillBroken=" + string.Join(", ", result.StillBroken));
            return result;
        }

        /// <summary>
        /// Fallback when some types are still wrong: Office Click-to-Run → offer Quick Repair; MSI Office → explain;
        /// no Office → Windows will ask which app to use the first time such a file is opened.
        /// </summary>
        public static string OfferFallback(IWin32Window owner, SystemRepairResult r)
        {
            if (r.StillBroken.Count == 0) return null;
            string list = string.Join(", ", r.StillBroken);
            string exe, platform, culture;
            if (FileAssocRestorer.IsClickToRunOffice(out exe, out platform, out culture))
            {
                if (MessageBox.Show(owner,
                        "These file types could not be restored to Microsoft Office:\n" + list +
                        "\n\nRun Office Quick Repair now? It re-registers all Office file types and icons (a few minutes, Office apps will be closed, no internet needed).",
                        "Restore Office file types", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    return FileAssocRestorer.StartOfficeQuickRepair() ? "Office Quick Repair started" : "Office Quick Repair could not start";
                return "still wrong: " + list;
            }
            bool officeAppsPresent = r.OfficeTypes > 0;
            return officeAppsPresent
                ? "still wrong: " + list + " - repair Office in Settings > Apps > Microsoft Office > Modify"
                : "still wrong: " + list + " - choose an app with Open with > Always";
        }
    }
}
