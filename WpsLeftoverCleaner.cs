using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace frmwpsoffice
{
    /// <summary>
    /// Leftovers the WPS uninstaller often keeps: scheduled tasks, autostart entries, the "WPS Cloud" item in
    /// Explorer's navigation pane, shell extensions / overlay icons and orphaned "Programs and Features" entries.
    /// Everything is matched by its target path (Kingsoft / WPS executables), never by a loose name.
    /// </summary>
    internal static class WpsLeftoverCleaner
    {
        private static readonly string[] RunKeys =
        {
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run",
        };

        private static readonly string[] NameSpaceKeys =
        {
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace",
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace",
        };

        private static readonly string[] ShellExRoots = { "*", "Directory", @"Directory\Background", "Folder", "AllFilesystemObjects", "Drive" };
        private static readonly string[] ShellExKinds = { "ContextMenuHandlers", "DragDropHandlers", "PropertySheetHandlers", "CopyHookHandlers" };

        // ------------------------------------------------------------------ machine part (elevated)

        public static void CleanMachine(RepairReport report)
        {
            CleanScheduledTasks(report);
            foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
            {
                string tag = view == RegistryView.Registry64 ? "HKLM" : "HKLM32";
                CleanRunKeys(hklm, tag, report);
                CleanUninstallEntries(hklm, tag, report);
                using (RegistryKey classes = hklm.OpenSubKey(@"SOFTWARE\Classes", true))
                {
                    if (classes == null) continue;
                    CleanNameSpace(hklm, classes, tag, report);
                    CleanShellExtensions(hklm, classes, tag, report);
                }
            }
            CleanServices(report);
        }

        // ------------------------------------------------------------------ user part (in-process)

        public static void CleanUser(RepairReport report)
        {
            RegistryKey hkcu = Registry.CurrentUser;
            CleanRunKeys(hkcu, "HKCU", report);
            CleanUninstallEntries(hkcu, "HKCU", report);
            using (RegistryKey classes = hkcu.CreateSubKey(@"Software\Classes"))
            {
                CleanNameSpace(hkcu, classes, "HKCU", report);
                CleanShellExtensions(hkcu, classes, "HKCU", report);
            }
        }

        // ------------------------------------------------------------------ pieces

        private static void CleanRunKeys(RegistryKey root, string tag, RepairReport report)
        {
            foreach (string path in RunKeys)
            {
                using (RegistryKey k = OpenWritable(root, path))
                {
                    if (k == null) continue;
                    foreach (string name in k.GetValueNames())
                    {
                        object v = k.GetValue(name);
                        // StartupApproved holds binary flags; match it by the name of a Run value we removed / WPS names.
                        bool wps = v is string ? WpsTraces.LooksLikeWpsPath((string)v)
                                   : name.IndexOf("wps", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("kingsoft", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (wps) WpsTraces.DeleteValue(k, name, tag + " autostart " + name, report);
                    }
                }
            }
        }

        /// <summary>Removes "Programs and Features" entries of WPS whose uninstaller is already gone.</summary>
        private static void CleanUninstallEntries(RegistryKey root, string tag, RepairReport report)
        {
            using (RegistryKey un = OpenWritable(root, @"Software\Microsoft\Windows\CurrentVersion\Uninstall"))
            {
                if (un == null) return;
                foreach (string sub in un.GetSubKeyNames())
                {
                    string name, publisher, uninstall;
                    using (RegistryKey k = un.OpenSubKey(sub))
                    {
                        if (k == null) continue;
                        name = k.GetValue("DisplayName") as string ?? "";
                        publisher = k.GetValue("Publisher") as string ?? "";
                        uninstall = k.GetValue("UninstallString") as string ?? "";
                    }
                    bool wps = name.IndexOf("WPS Office", StringComparison.OrdinalIgnoreCase) >= 0
                               || publisher.IndexOf("Kingsoft", StringComparison.OrdinalIgnoreCase) >= 0
                               || WpsTraces.LooksLikeWpsPath(uninstall);
                    if (wps && WpsTraces.FileMissing(uninstall))
                        WpsTraces.DeleteTree(un, sub, tag + " uninstall entry '" + (name.Length > 0 ? name : sub) + "'", report);
                }
            }
        }

        /// <summary>"WPS Cloud" / "WPS网盘" items in Explorer's navigation pane and This PC.</summary>
        private static void CleanNameSpace(RegistryKey root, RegistryKey classes, string tag, RepairReport report)
        {
            foreach (string path in NameSpaceKeys)
            {
                using (RegistryKey ns = OpenWritable(root, path))
                {
                    if (ns == null) continue;
                    foreach (string clsid in ns.GetSubKeyNames())
                    {
                        string label;
                        using (RegistryKey e = ns.OpenSubKey(clsid)) label = e?.GetValue("") as string ?? "";
                        if (!IsWpsClsid(classes, clsid, label)) continue;
                        WpsTraces.DeleteTree(ns, clsid, tag + " Explorer item " + clsid + " " + label, report);
                        DeleteClsid(classes, clsid, tag, report);
                        using (RegistryKey hide = OpenWritable(root, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel"))
                            if (hide != null && hide.GetValue(clsid) != null) hide.DeleteValue(clsid, false);
                    }
                }
            }
        }

        /// <summary>Context-menu handlers and overlay icons whose DLL is a WPS/Kingsoft one.</summary>
        private static void CleanShellExtensions(RegistryKey root, RegistryKey classes, string tag, RepairReport report)
        {
            var removed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string r in ShellExRoots)
            foreach (string kind in ShellExKinds)
            {
                using (RegistryKey handlers = OpenWritable(classes, r + @"\shellex\" + kind))
                {
                    if (handlers == null) continue;
                    foreach (string name in handlers.GetSubKeyNames())
                    {
                        string clsid;
                        using (RegistryKey h = handlers.OpenSubKey(name)) clsid = h?.GetValue("") as string;
                        if (string.IsNullOrEmpty(clsid) || !clsid.StartsWith("{")) clsid = name;
                        if (!IsWpsClsid(classes, clsid, name)) continue;
                        WpsTraces.DeleteTree(handlers, name, tag + " " + r + " " + kind + " " + name, report);
                        removed.Add(clsid);
                    }
                }
            }

            using (RegistryKey overlays = OpenWritable(root, @"Software\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers"))
            {
                if (overlays != null)
                {
                    foreach (string name in overlays.GetSubKeyNames())
                    {
                        string clsid;
                        using (RegistryKey o = overlays.OpenSubKey(name)) clsid = o?.GetValue("") as string ?? "";
                        if (!IsWpsClsid(classes, clsid, name)) continue;
                        WpsTraces.DeleteTree(overlays, name, tag + " overlay icon " + name.Trim(), report);
                        removed.Add(clsid);
                    }
                }
            }

            using (RegistryKey approved = OpenWritable(root, @"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved"))
            {
                foreach (string clsid in removed)
                {
                    if (approved != null && approved.GetValue(clsid) != null) approved.DeleteValue(clsid, false);
                    DeleteClsid(classes, clsid, tag, report);
                }
            }
        }

        /// <summary>
        /// WPS CLSID: its server DLL/EXE is in a WPS folder, or (already uninstalled, CLSID gone) the entry's own label names WPS.
        /// </summary>
        private static bool IsWpsClsid(RegistryKey classes, string clsid, string label)
        {
            if (string.IsNullOrEmpty(clsid)) return false;
            string server = null, name = null;
            bool exists = false;
            try
            {
                foreach (string prefix in new[] { "", @"WOW6432Node\" })
                {
                    using (RegistryKey c = classes.OpenSubKey(prefix + @"CLSID\" + clsid))
                    {
                        if (c == null) continue;
                        exists = true;
                        name = name ?? c.GetValue("") as string;
                        using (RegistryKey s = c.OpenSubKey("InprocServer32") ?? c.OpenSubKey("LocalServer32"))
                            server = server ?? s?.GetValue("") as string;
                    }
                }
            }
            catch { }
            if (WpsTraces.LooksLikeWpsPath(server)) return true;
            if (exists && !string.IsNullOrEmpty(server)) return false; // belongs to some other program
            return LabelIsWps(label) || LabelIsWps(name);
        }

        private static bool LabelIsWps(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            return s.IndexOf("WPS", StringComparison.Ordinal) >= 0 || s.IndexOf("Kingsoft", StringComparison.OrdinalIgnoreCase) >= 0
                   || s.IndexOf("kso", StringComparison.OrdinalIgnoreCase) == 0;
        }

        private static void DeleteClsid(RegistryKey classes, string clsid, string tag, RepairReport report)
        {
            if (string.IsNullOrEmpty(clsid) || !clsid.StartsWith("{")) return;
            foreach (string prefix in new[] { "", @"WOW6432Node\" })
                using (RegistryKey parent = OpenWritable(classes, prefix + "CLSID"))
                    if (parent != null) WpsTraces.DeleteTree(parent, clsid, tag + " " + prefix + "CLSID " + clsid, report);
        }

        /// <summary>Scheduled tasks (WpsUpdateTask_*, WpsExternal_*, ...) whose action runs a WPS program.</summary>
        private static void CleanScheduledTasks(RepairReport report)
        {
            // A 32-bit process would be redirected to SysWOW64, which has no Tasks folder.
            string sys = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess ? "Sysnative" : "System32";
            string tasksDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), sys, "Tasks");
            IEnumerable<string> files;
            try { files = Directory.EnumerateFiles(tasksDir, "*", SearchOption.AllDirectories).ToList(); }
            catch (Exception ex) { report.Fail("Cannot read scheduled tasks: " + ex.Message); return; }

            foreach (string file in files)
            {
                string xml;
                try { xml = File.ReadAllText(file); } catch { continue; }
                int start = xml.IndexOf("<Actions", StringComparison.OrdinalIgnoreCase);
                string actions = start >= 0 ? xml.Substring(start) : xml;
                if (!WpsTraces.LooksLikeWpsPath(actions)) continue;

                string taskName = "\\" + file.Substring(tasksDir.Length).TrimStart('\\');
                int rc = Run("schtasks.exe", "/Delete /TN \"" + taskName + "\" /F");
                if (rc == 0) report.Done("Deleted scheduled task " + taskName);
                else
                {
                    // Task file without a registry entry (orphan): remove the file itself.
                    try { File.Delete(file); report.Done("Deleted orphan task file " + taskName); }
                    catch (Exception ex) { report.Fail("Cannot delete scheduled task " + taskName + ": " + ex.Message); }
                }
            }
        }

        /// <summary>WPS services (cloud/update) whose program file is already gone.</summary>
        private static void CleanServices(RepairReport report)
        {
            using (RegistryKey services = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services"))
            {
                if (services == null) return;
                foreach (string name in services.GetSubKeyNames())
                {
                    string image;
                    try { using (RegistryKey s = services.OpenSubKey(name)) image = s?.GetValue("ImagePath") as string; }
                    catch { continue; }
                    if (!WpsTraces.LooksLikeWpsPath(image)) continue;
                    Run("sc.exe", "stop \"" + name + "\"");
                    int rc = Run("sc.exe", "delete \"" + name + "\"");
                    if (rc == 0 || rc == 1072 /* marked for deletion */) report.Done("Deleted service " + name);
                    else report.Fail("Cannot delete service " + name + " (sc rc=" + rc + ")");
                }
            }
        }

        // ------------------------------------------------------------------ helpers

        private static RegistryKey OpenWritable(RegistryKey root, string path)
        {
            try { return root.OpenSubKey(path, true); }
            catch { return null; }
        }

        private static int Run(string exe, string args)
        {
            try
            {
                using (Process p = Process.Start(new ProcessStartInfo(exe, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }))
                {
                    if (p == null) return -1;
                    p.StandardOutput.ReadToEnd();
                    p.StandardError.ReadToEnd();
                    if (!p.WaitForExit(30000)) return -1;
                    return p.ExitCode;
                }
            }
            catch { return -1; }
        }
    }
}
