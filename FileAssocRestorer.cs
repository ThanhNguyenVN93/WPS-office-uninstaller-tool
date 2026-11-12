using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace frmwpsoffice
{
    /// <summary>
    /// Undoes WPS's take-over of the Office file types:
    ///  - MS Office installed  → the extension points to the Office ProgID again (icons + double-click open Office).
    ///  - Office not installed → WPS's association is removed, so Windows is back to its own state
    ///    ("How do you want to open this file?" / another app the user already had).
    /// Machine part (HKLM\Software\Classes) needs admin and runs in the elevated helper;
    /// user part (HKCU) runs in the app itself, so it always hits the real user's hive.
    /// </summary>
    internal static class FileAssocRestorer
    {
        /// <summary>Extension → MS Office ProgID (same values Office 2016/365 Click-to-Run registers).</summary>
        public static readonly Dictionary<string, string> OfficeProgIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".doc", "Word.Document.8" }, { ".docx", "Word.Document.12" }, { ".docm", "Word.DocumentMacroEnabled.12" },
            { ".dot", "Word.Template.8" }, { ".dotx", "Word.Template.12" }, { ".dotm", "Word.TemplateMacroEnabled.12" },
            { ".rtf", "Word.RTF.8" }, { ".odt", "Word.OpenDocumentText.12" },
            { ".xls", "Excel.Sheet.8" }, { ".xlsx", "Excel.Sheet.12" }, { ".xlsm", "Excel.SheetMacroEnabled.12" },
            { ".xlsb", "Excel.SheetBinaryMacroEnabled.12" }, { ".xlt", "Excel.Template.8" }, { ".xltx", "Excel.Template" },
            { ".xltm", "Excel.TemplateMacroEnabled" }, { ".csv", "Excel.CSV" }, { ".ods", "Excel.OpenDocumentSpreadsheet.12" },
            { ".ppt", "PowerPoint.Show.8" }, { ".pptx", "PowerPoint.Show.12" }, { ".pptm", "PowerPoint.ShowMacroEnabled.12" },
            { ".pps", "PowerPoint.SlideShow.8" }, { ".ppsx", "PowerPoint.SlideShow.12" }, { ".ppsm", "PowerPoint.SlideShowMacroEnabled.12" },
            { ".pot", "PowerPoint.Template.8" }, { ".potx", "PowerPoint.Template.12" }, { ".potm", "PowerPoint.TemplateMacroEnabled.12" },
            { ".odp", "PowerPoint.OpenDocumentPresentation.12" },
        };

        /// <summary>Other types WPS takes over that have no Office counterpart (only cleaned, never re-pointed).</summary>
        private static readonly string[] OtherExtensions = { ".pdf", ".wps", ".wpt", ".et", ".ett", ".dps", ".dpt", ".txt", ".xml", ".ofd" };

        /// <summary>WPS's own formats: nothing else registers them, so a dangling association there is WPS's leftover.</summary>
        private static readonly string[] WpsNativeExtensions = { ".wps", ".wpt", ".et", ".ett", ".dps", ".dpt" };

        private static IEnumerable<string> AllExtensions { get { return OfficeProgIds.Keys.Concat(OtherExtensions); } }

        /// <summary>
        /// Office and WPS-native types: WPS ProgID or a dangling one (left by the WPS uninstaller) is bad.
        /// Shared types (.pdf, .txt, .xml): only an association that really points to WPS is touched.
        /// </summary>
        private static bool IsBad(string ext, RegistryKey lookup, string progId)
        {
            if (string.IsNullOrEmpty(progId)) return false;
            bool ownedType = OfficeProgIds.ContainsKey(ext) || WpsNativeExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
            return ownedType ? WpsTraces.IsBrokenOrWps(lookup, progId) : WpsTraces.IsWpsProgId(lookup, progId);
        }

        private const string FileExtsPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts";

        // ------------------------------------------------------------------ Office detection

        /// <summary>True when the Office ProgID exists and really opens an Office program that is on disk.</summary>
        public static bool OfficeProgIdUsable(RegistryKey classes, string progId)
        {
            List<string> cmds = WpsTraces.ProgIdCommands(classes, progId).ToList();
            if (cmds.Count == 0 || cmds.Any(WpsTraces.LooksLikeWpsPath)) return false;
            return cmds.Any(c => !WpsTraces.FileMissing(c));
        }

        /// <summary>Office ProgID that exists but was rewritten to run WPS (only an Office repair can bring the real one back).</summary>
        private static bool OfficeProgIdHijacked(RegistryKey classes, string progId)
        {
            return WpsTraces.ProgIdCommands(classes, progId).Any(WpsTraces.LooksLikeWpsPath);
        }

        public static bool IsClickToRunOffice(out string clientExe, out string platform, out string culture)
        {
            clientExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
                @"Microsoft Shared\ClickToRun\OfficeClickToRun.exe");
            platform = culture = null;
            try
            {
                using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (RegistryKey cfg = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration"))
                {
                    if (cfg == null) return false;
                    platform = cfg.GetValue("Platform") as string;
                    culture = cfg.GetValue("ClientCulture") as string;
                }
            }
            catch { return false; }
            return File.Exists(clientExe) && !string.IsNullOrEmpty(platform);
        }

        // ------------------------------------------------------------------ machine part (elevated)

        public static void RestoreMachine(RepairReport report)
        {
            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (RegistryKey classes = hklm.OpenSubKey(@"SOFTWARE\Classes", true))
            {
                if (classes == null) { report.Fail("HKLM\\SOFTWARE\\Classes not writable"); return; }
                var wpsProgIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                FixClassesRoot(classes, "HKLM", report, wpsProgIds, true);
                DeleteWpsProgIds(classes, "HKLM", report, wpsProgIds);
                CleanRegisteredApplications(hklm, report);
            }
        }

        // ------------------------------------------------------------------ user part (in-process, real user)

        /// <param name="machineFixed">False when the elevated part did not run (UAC declined): then the Office ProgID
        /// is also written per-user, so at least this user gets Office back without admin rights.</param>
        public static void RestoreUser(RepairReport report, bool machineFixed)
        {
            using (RegistryKey classes = Registry.CurrentUser.CreateSubKey(@"Software\Classes"))
            {
                var wpsProgIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                FixClassesRoot(classes, "HKCU", report, wpsProgIds, false);

                if (!machineFixed)
                {
                    // Per-user fallback: point the extension to Office in HKCU when Office is installed.
                    foreach (var pair in OfficeProgIds)
                    {
                        if (!OfficeProgIdUsable(Registry.ClassesRoot, pair.Value)) continue;
                        string machineDefault = null;
                        using (RegistryKey m = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\" + pair.Key))
                            if (m != null) machineDefault = m.GetValue("") as string;
                        if (machineDefault == null || !WpsTraces.IsBrokenOrWps(Registry.ClassesRoot, machineDefault)) continue;
                        try
                        {
                            using (RegistryKey ext = classes.CreateSubKey(pair.Key)) ext.SetValue("", pair.Value);
                            report.Done("HKCU " + pair.Key + " -> " + pair.Value + " (per-user fallback)");
                        }
                        catch (Exception ex) { report.Fail("HKCU " + pair.Key + ": " + ex.Message); }
                    }
                }

                FixUserChoices(report, wpsProgIds);
                DeleteWpsProgIds(classes, "HKCU", report, wpsProgIds);
            }
        }

        // ------------------------------------------------------------------ shared logic

        /// <summary>Fixes the extension keys under one Classes root and collects the WPS ProgIDs it saw.</summary>
        private static void FixClassesRoot(RegistryKey classes, string root, RepairReport report, HashSet<string> wpsProgIds, bool machine)
        {
            // Resolve ProgIDs against the merged view so an HKCU check also sees machine ProgIDs and vice versa.
            RegistryKey lookup = machine ? classes : Registry.ClassesRoot;

            foreach (string ext in AllExtensions)
            {
                RegistryKey key;
                try { key = classes.OpenSubKey(ext, true); }
                catch (Exception ex) { report.Fail(root + " " + ext + ": " + ex.Message); continue; }
                if (key == null) continue;
                using (key)
                {
                    string current = key.GetValue("") as string;
                    string office;
                    OfficeProgIds.TryGetValue(ext, out office);

                    // Pointer to the Office ProgID is kept while that ProgID exists; a dangling one (no Office) is cleared.
                    bool isOfficeId = office != null && string.Equals(current, office, StringComparison.OrdinalIgnoreCase)
                                      && WpsTraces.ProgIdExists(lookup, office);
                    bool currentBad = !isOfficeId && IsBad(ext, lookup, current);
                    // The Office ProgID itself was rewritten to run WPS: keep the pointer, only an Office repair restores it.
                    if (isOfficeId && machine && OfficeProgIdHijacked(lookup, office))
                        report.Fail(root + " " + office + " runs WPS (Office repair needed)");
                    if (currentBad && WpsTraces.IsWpsProgId(lookup, current)) wpsProgIds.Add(current);

                    if (currentBad)
                    {
                        try
                        {
                            if (machine && office != null && OfficeProgIdUsable(lookup, office))
                            {
                                key.SetValue("", office);
                                report.Done(root + " " + ext + ": " + current + " -> " + office);
                            }
                            else
                            {
                                // No Office for this type (or per-user key): drop the override, Windows decides again.
                                key.DeleteValue("", false);
                                report.Done(root + " " + ext + ": removed association " + current);
                            }
                        }
                        catch (Exception ex) { report.Fail(root + " " + ext + ": " + ex.Message); }
                    }

                    CleanOpenWithProgids(key, lookup, root + " " + ext, report, wpsProgIds);

                    // WPS keeps its "New > WPS ..." entry in .ext\<WPS ProgID>\ShellNew or .ext\ShellNew.
                    foreach (string sub in key.GetSubKeyNames())
                    {
                        if (WpsTraces.HasWpsProgIdName(sub))
                            WpsTraces.DeleteTree(key, sub, root + " " + ext + "\\" + sub, report);
                    }
                    using (RegistryKey shellNew = key.OpenSubKey("ShellNew"))
                    {
                        bool wpsNew = shellNew != null && shellNew.GetValueNames().Any(n => WpsTraces.LooksLikeWpsPath(shellNew.GetValue(n) as string));
                        if (wpsNew) WpsTraces.DeleteTree(key, "ShellNew", root + " " + ext + "\\ShellNew", report);
                    }
                }
            }

            foreach (string exe in WpsTraces.AppExeNames)
                WpsTraces.DeleteTree(classes, @"Applications\" + exe, root + @" Classes\Applications\" + exe, report);
        }

        private static void CleanOpenWithProgids(RegistryKey extKey, RegistryKey lookup, string display, RepairReport report, HashSet<string> wpsProgIds)
        {
            RegistryKey owp;
            try { owp = extKey.OpenSubKey("OpenWithProgids", true); }
            catch { return; }
            if (owp == null) return;
            using (owp)
            {
                foreach (string name in owp.GetValueNames())
                {
                    if (name.Length == 0) continue;
                    // Only names WPS uses or ProgIDs that run WPS; a missing non-WPS ProgID is left alone.
                    if (WpsTraces.HasWpsProgIdName(name) || (WpsTraces.ProgIdExists(lookup, name) && WpsTraces.IsWpsProgId(lookup, name)))
                    {
                        wpsProgIds.Add(name);
                        WpsTraces.DeleteValue(owp, name, display + " OpenWithProgids " + name, report);
                    }
                }
            }
        }

        /// <summary>Explorer's per-user choice (FileExts\.ext\UserChoice / UserChoiceLatest) is hash protected and cannot be
        /// written, but deleting it makes Windows fall back to the Classes default fixed above.</summary>
        private static void FixUserChoices(RepairReport report, HashSet<string> wpsProgIds)
        {
            using (RegistryKey fileExts = Registry.CurrentUser.OpenSubKey(FileExtsPath, true))
            {
                if (fileExts == null) return;
                foreach (string ext in AllExtensions)
                {
                    RegistryKey key;
                    try { key = fileExts.OpenSubKey(ext, true); }
                    catch { continue; }
                    if (key == null) continue;
                    using (key)
                    {
                        foreach (string choiceName in new[] { "UserChoice", "UserChoiceLatest" })
                        {
                            string progId = null;
                            using (RegistryKey choice = key.OpenSubKey(choiceName))
                            {
                                if (choice == null) continue;
                                progId = choice.GetValue("ProgId") as string;
                                // UserChoiceLatest keeps the ProgId one level deeper on some builds.
                                if (progId == null)
                                    using (RegistryKey inner = choice.OpenSubKey("ProgId"))
                                        if (inner != null) progId = inner.GetValue("ProgId") as string;
                            }
                            if (!IsBad(ext, Registry.ClassesRoot, progId)) continue;
                            if (WpsTraces.HasWpsProgIdName(progId)) wpsProgIds.Add(progId);
                            try
                            {
                                key.DeleteSubKeyTree(choiceName, false);
                                report.Done("HKCU FileExts " + ext + "\\" + choiceName + " (" + progId + ") removed");
                            }
                            catch (Exception ex)
                            {
                                // Windows 11 UCPD protects some types (.pdf): the user must pick the app once in Settings.
                                report.Fail("HKCU FileExts " + ext + "\\" + choiceName + " (" + progId + ") locked by Windows: " + ex.Message);
                            }
                        }

                        CleanOpenWithProgids(key, Registry.ClassesRoot, "HKCU FileExts " + ext, report, wpsProgIds);
                        CleanOpenWithList(key, "HKCU FileExts " + ext, report);
                    }
                }
            }
        }

        private static void CleanOpenWithList(RegistryKey extKey, string display, RepairReport report)
        {
            RegistryKey owl;
            try { owl = extKey.OpenSubKey("OpenWithList", true); }
            catch { return; }
            if (owl == null) return;
            using (owl)
            {
                string mru = owl.GetValue("MRUList") as string ?? "";
                foreach (string name in owl.GetValueNames())
                {
                    if (name == "MRUList") continue;
                    string exe = owl.GetValue(name) as string;
                    if (exe == null || !WpsTraces.AppExeNames.Any(a => a.Equals(exe, StringComparison.OrdinalIgnoreCase))) continue;
                    WpsTraces.DeleteValue(owl, name, display + " OpenWithList " + exe, report);
                    if (name.Length == 1) mru = mru.Replace(name, "");
                }
                try { if (owl.GetValue("MRUList") != null) owl.SetValue("MRUList", mru); } catch { }
            }
        }

        /// <summary>Deletes WPS ProgID keys: those found above plus every key named like a WPS ProgID.</summary>
        private static void DeleteWpsProgIds(RegistryKey classes, string root, RepairReport report, HashSet<string> found)
        {
            var targets = new HashSet<string>(found, StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (string name in classes.GetSubKeyNames())
                    if (!name.StartsWith(".") && WpsTraces.HasWpsProgIdName(name)) targets.Add(name);
            }
            catch (Exception ex) { report.Fail(root + " cannot list Classes: " + ex.Message); }

            foreach (string progId in targets)
            {
                if (progId.StartsWith(@"Applications\", StringComparison.OrdinalIgnoreCase)) continue;
                // Never delete an Office ProgID here (a hijacked one is reported for Office repair instead).
                if (OfficeProgIds.Values.Contains(progId, StringComparer.OrdinalIgnoreCase)) continue;
                WpsTraces.DeleteTree(classes, progId, root + " ProgID " + progId, report);
            }
        }

        private static void CleanRegisteredApplications(RegistryKey hklm, RepairReport report)
        {
            using (RegistryKey reg = hklm.OpenSubKey(@"SOFTWARE\RegisteredApplications", true))
            {
                if (reg == null) return;
                foreach (string name in reg.GetValueNames())
                {
                    string data = reg.GetValue(name) as string ?? "";
                    if (data.IndexOf("Kingsoft", StringComparison.OrdinalIgnoreCase) >= 0 || name.StartsWith("WPS", StringComparison.OrdinalIgnoreCase))
                        WpsTraces.DeleteValue(reg, name, "RegisteredApplications " + name, report);
                }
            }
        }

        // ------------------------------------------------------------------ verification + refresh

        /// <summary>Extensions whose effective association (what Explorer uses) still points to WPS or to nothing valid.</summary>
        public static List<string> StillBroken()
        {
            var broken = new List<string>();
            foreach (string ext in AllExtensions)
            {
                string progId = null;
                try
                {
                    foreach (string choiceName in new[] { "UserChoiceLatest", "UserChoice" })
                    {
                        using (RegistryKey c = Registry.CurrentUser.OpenSubKey(FileExtsPath + "\\" + ext + "\\" + choiceName))
                        {
                            if (c == null) continue;
                            progId = c.GetValue("ProgId") as string;
                            if (progId == null)
                                using (RegistryKey inner = c.OpenSubKey("ProgId"))
                                    if (inner != null) progId = inner.GetValue("ProgId") as string;
                        }
                        if (progId != null) break;
                    }
                    if (progId == null)
                        using (RegistryKey k = Registry.ClassesRoot.OpenSubKey(ext))
                            if (k != null) progId = k.GetValue("") as string;
                }
                catch { }
                if (IsBad(ext, Registry.ClassesRoot, progId))
                    broken.Add(ext + " (" + progId + ")");
                else if (progId != null && OfficeProgIds.ContainsKey(ext) && OfficeProgIdHijacked(Registry.ClassesRoot, progId))
                    broken.Add(ext + " (" + progId + " runs WPS)");
            }
            return broken;
        }

        /// <summary>Count of Office types that now open with Office.</summary>
        public static int CountOnOffice()
        {
            int n = 0;
            foreach (var pair in OfficeProgIds)
            {
                using (RegistryKey k = Registry.ClassesRoot.OpenSubKey(pair.Key))
                {
                    string cur = k == null ? null : k.GetValue("") as string;
                    if (string.Equals(cur, pair.Value, StringComparison.OrdinalIgnoreCase) && OfficeProgIdUsable(Registry.ClassesRoot, pair.Value)) n++;
                }
            }
            return n;
        }

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        /// <summary>Tells Explorer the associations changed and rebuilds the icon cache.</summary>
        public static void RefreshShell()
        {
            try { SHChangeNotify(0x08000000 /* SHCNE_ASSOCCHANGED */, 0, IntPtr.Zero, IntPtr.Zero); }
            catch (Exception ex) { Logger.Warn("SHChangeNotify failed: " + ex.Message); }
            try
            {
                string ie4 = Path.Combine(Environment.SystemDirectory, "ie4uinit.exe");
                if (File.Exists(ie4))
                    using (Process p = Process.Start(new ProcessStartInfo(ie4, "-show") { UseShellExecute = false, CreateNoWindow = true }))
                        p?.WaitForExit(10000);
            }
            catch (Exception ex) { Logger.Warn("ie4uinit failed: " + ex.Message); }
        }

        /// <summary>Fallback: Office Click-to-Run Quick Repair rewrites every Office association and icon. Runs in the background.</summary>
        public static bool StartOfficeQuickRepair()
        {
            string exe, platform, culture;
            if (!IsClickToRunOffice(out exe, out platform, out culture)) return false;
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = "scenario=Repair platform=" + platform + " culture=" + (culture ?? "en-us") +
                                " RepairType=QuickRepair forceappshutdown=True DisplayLevel=True",
                    UseShellExecute = true,
                    Verb = "runas"
                });
                Logger.Info("Started Office Quick Repair");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn("Office Quick Repair could not start: " + ex.Message);
                return false;
            }
        }
    }
}
