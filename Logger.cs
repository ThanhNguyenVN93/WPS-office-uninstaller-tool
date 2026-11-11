using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace frmwpsoffice
{
    /// <summary>
    /// Tiny local file logger: %AppData%\WPSOfficeUninstaller\logs\app-yyyyMMdd.log (last 5 files kept).
    /// Nothing is ever sent anywhere; the file is only for the user to attach to a bug report.
    /// </summary>
    internal static class Logger
    {
        private const int KeepFiles = 5;
        private static readonly object Sync = new object();
        private static bool _pruned;

        public static string LogDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WPSOfficeUninstaller", "logs");

        public static string CurrentFile
        {
            get { return Path.Combine(LogDirectory, "app-" + DateTime.Now.ToString("yyyyMMdd") + ".log"); }
        }

        public static void Info(string message) { Write("INFO ", message); }
        public static void Warn(string message) { Write("WARN ", message); }

        public static void Error(string message, Exception ex = null)
        {
            Write("ERROR", ex == null ? message : message + " | " + ex.GetType().Name + ": " + ex.Message + Environment.NewLine + ex.StackTrace);
        }

        public static void LogStartup()
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            bool admin = false;
            try
            {
                using (var id = System.Security.Principal.WindowsIdentity.GetCurrent())
                    admin = new System.Security.Principal.WindowsPrincipal(id).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch { }
            Info("==== Start v" + v.ToString(3) + " | " + Environment.OSVersion.VersionString + " | " +
                 (Environment.Is64BitOperatingSystem ? "x64" : "x86") + " | CLR " + Environment.Version + " | admin=" + admin);
        }

        /// <summary>Opens the log folder in Explorer (creating it if needed).</summary>
        public static void OpenFolder()
        {
            try
            {
                Directory.CreateDirectory(LogDirectory);
                string file = CurrentFile;
                if (File.Exists(file))
                    Process.Start("explorer.exe", "/select,\"" + file + "\"");
                else
                    Process.Start("explorer.exe", "\"" + LogDirectory + "\"");
            }
            catch { }
        }

        private static void Write(string level, string message)
        {
            try
            {
                lock (Sync)
                {
                    Directory.CreateDirectory(LogDirectory);
                    if (!_pruned)
                    {
                        _pruned = true;
                        Prune();
                    }
                    File.AppendAllText(CurrentFile,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + level + "] " + message + Environment.NewLine);
                }
            }
            catch
            {
                // Logging must never break the app.
            }
        }

        private static void Prune()
        {
            try
            {
                foreach (string old in Directory.GetFiles(LogDirectory, "app-*.log")
                                                .OrderByDescending(f => f, StringComparer.Ordinal)
                                                .Skip(KeepFiles))
                {
                    try { File.Delete(old); } catch { }
                }
            }
            catch { }
        }
    }
}
