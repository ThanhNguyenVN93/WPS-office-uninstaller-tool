using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace frmwpsoffice
{
    /// <summary>Collects what a repair step did; the elevated helper sends it back to the main window as text.</summary>
    internal sealed class RepairReport
    {
        public readonly List<string> Lines = new List<string>();
        public int Changed;
        public int Failed;

        public void Done(string message) { Changed++; Lines.Add("INFO  " + message); }
        public void Note(string message) { Lines.Add("INFO  " + message); }
        public void Fail(string message) { Failed++; Lines.Add("WARN  " + message); }

        public void WriteTo(string path)
        {
            File.WriteAllLines(path, new[] { "CHANGED=" + Changed, "FAILED=" + Failed }.Concat(Lines));
        }

        /// <summary>Reads a report written by <see cref="WriteTo"/>; returns null when missing.</summary>
        public static RepairReport ReadFrom(string path)
        {
            if (!File.Exists(path)) return null;
            var r = new RepairReport();
            foreach (string line in File.ReadAllLines(path))
            {
                int n;
                if (line.StartsWith("CHANGED=") && int.TryParse(line.Substring(8), out n)) r.Changed = n;
                else if (line.StartsWith("FAILED=") && int.TryParse(line.Substring(7), out n)) r.Failed = n;
                else if (line.Length > 0) r.Lines.Add(line);
            }
            return r;
        }

        public void Merge(RepairReport other)
        {
            if (other == null) return;
            Changed += other.Changed;
            Failed += other.Failed;
            Lines.AddRange(other.Lines);
        }

        public void FlushToLog(string prefix)
        {
            foreach (string l in Lines)
            {
                if (l.StartsWith("WARN")) Logger.Warn(prefix + l.Substring(6));
                else Logger.Info(prefix + (l.Length > 6 ? l.Substring(6) : l));
            }
        }
    }

    /// <summary>Recognises registry data left by WPS Office / Kingsoft.</summary>
    internal static class WpsTraces
    {
        /// <summary>ProgID prefixes WPS registers for its file types (KWPS = Writer, KET = Spreadsheets, KWPP = Presentation).</summary>
        private static readonly string[] ProgIdPrefixes = { "KWPS.", "KET.", "KWPP.", "WPS.", "WPSPDF.", "KWPSPDF." };

        private static readonly string[] PathMarkers =
        {
            @"\kingsoft\", @"\wps office\", "ksolaunch.exe", "wpscloudsvr", "wpscenter", "wpsupdate", "wpspdf.exe",
            @"\wps.exe", @"\et.exe", @"\wpp.exe", "qingshellext", "kbaseconfigcenter"
        };

        /// <summary>Exe names WPS registers under Classes\Applications and OpenWithList.</summary>
        public static readonly string[] AppExeNames = { "wps.exe", "et.exe", "wpp.exe", "wpspdf.exe", "ksolaunch.exe", "wpsoffice.exe" };

        public static bool LooksLikeWpsPath(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (string m in PathMarkers)
                if (text.IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        public static bool HasWpsProgIdName(string progId)
        {
            if (string.IsNullOrEmpty(progId)) return false;
            if (progId.StartsWith(@"Applications\", StringComparison.OrdinalIgnoreCase))
                return AppExeNames.Any(a => progId.EndsWith(a, StringComparison.OrdinalIgnoreCase));
            return ProgIdPrefixes.Any(p => progId.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Every command line / icon path found under a ProgID (all shell verbs + DefaultIcon).</summary>
        public static IEnumerable<string> ProgIdCommands(RegistryKey classes, string progId)
        {
            var result = new List<string>();
            try
            {
                using (RegistryKey k = classes.OpenSubKey(progId))
                {
                    if (k == null) return result;
                    using (RegistryKey icon = k.OpenSubKey("DefaultIcon"))
                        if (icon != null) result.Add(icon.GetValue("") as string);
                    using (RegistryKey shell = k.OpenSubKey("shell"))
                    {
                        if (shell == null) return result;
                        foreach (string verb in shell.GetSubKeyNames())
                            using (RegistryKey cmd = shell.OpenSubKey(verb + @"\command"))
                                if (cmd != null) result.Add(cmd.GetValue("") as string);
                    }
                }
            }
            catch { }
            return result.Where(s => !string.IsNullOrEmpty(s));
        }

        public static bool ProgIdExists(RegistryKey classes, string progId)
        {
            if (string.IsNullOrEmpty(progId)) return false;
            try { using (RegistryKey k = classes.OpenSubKey(progId)) return k != null; }
            catch { return false; }
        }

        /// <summary>True when the ProgID belongs to WPS (by name or because its commands run WPS).</summary>
        public static bool IsWpsProgId(RegistryKey classes, string progId)
        {
            if (HasWpsProgIdName(progId)) return true;
            return ProgIdCommands(classes, progId).Any(LooksLikeWpsPath);
        }

        /// <summary>
        /// "Bad" association target: a WPS ProgID, or a ProgID that no longer exists (WPS deleted it on uninstall and
        /// left the extension pointing at nothing, which is what breaks the icons).
        /// </summary>
        public static bool IsBrokenOrWps(RegistryKey classes, string progId)
        {
            if (string.IsNullOrEmpty(progId)) return false;
            if (HasWpsProgIdName(progId)) return true;
            if (!ProgIdExists(classes, progId)) return true;
            return IsWpsProgId(classes, progId);
        }

        /// <summary>First token of a command line ("C:\x y\a.exe" /p → C:\x y\a.exe), env vars expanded.</summary>
        public static string ExeOf(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return null;
            string c = Environment.ExpandEnvironmentVariables(command.Trim());
            if (c.StartsWith("\""))
            {
                int end = c.IndexOf('"', 1);
                return end > 1 ? c.Substring(1, end - 1) : null;
            }
            int exe = c.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            if (exe > 0) return c.Substring(0, exe + 4);
            int sp = c.IndexOf(' ');
            return sp > 0 ? c.Substring(0, sp) : c;
        }

        public static bool FileMissing(string command)
        {
            string exe = ExeOf(command);
            if (string.IsNullOrEmpty(exe)) return true;
            try { return !File.Exists(exe); } catch { return true; }
        }

        public static void DeleteTree(RegistryKey parent, string name, string display, RepairReport report)
        {
            try
            {
                using (RegistryKey k = parent.OpenSubKey(name))
                    if (k == null) return;
                parent.DeleteSubKeyTree(name, false);
                report.Done("Deleted " + display);
            }
            catch (Exception ex) { report.Fail("Cannot delete " + display + ": " + ex.Message); }
        }

        public static void DeleteValue(RegistryKey key, string name, string display, RepairReport report)
        {
            try
            {
                key.DeleteValue(name, false);
                report.Done("Deleted " + display);
            }
            catch (Exception ex) { report.Fail("Cannot delete " + display + ": " + ex.Message); }
        }
    }
}
