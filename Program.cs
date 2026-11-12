using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace frmwpsoffice
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Update step: this is the freshly downloaded exe replacing the installed one (no UI, no single-instance lock).
            if (args.Length >= 3 && args[0] == Updater.ApplyArg)
            {
                Updater.RunApply(args[1], args[2], args.Length > 3 && args[3] == Updater.ElevatedArg);
                return;
            }

            // Elevated helper for CLEAN UP: restore file associations + remove WPS leftovers (no UI).
            if (args.Length >= 2 && args[0] == SystemRepair.ElevatedArg)
            {
                Environment.Exit(SystemRepair.RunElevatedChild(args[1]));
                return;
            }

            // Single instance: a second launch just brings the running window to the front.
            bool createdNew;
            Mutex mutex = null;
            try
            {
                mutex = new Mutex(true, "Local\\WPSOfficeUninstallerTool_SingleInstance", out createdNew);
            }
            catch (UnauthorizedAccessException)
            {
                // Mutex owned by an elevated instance of the same app.
                createdNew = false;
            }

            if (!createdNew)
            {
                Logger.Info("Second instance blocked");
                ActivateRunningInstance();
                return;
            }

            try
            {
                RunApp();
            }
            finally
            {
                try { mutex.ReleaseMutex(); } catch { }
                mutex.Dispose();
            }
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static void ActivateRunningInstance()
        {
            try
            {
                Process current = Process.GetCurrentProcess();
                foreach (Process p in Process.GetProcessesByName(current.ProcessName))
                {
                    if (p.Id == current.Id || p.MainWindowHandle == IntPtr.Zero) continue;
                    ShowWindow(p.MainWindowHandle, 9); // SW_RESTORE
                    SetForegroundWindow(p.MainWindowHandle);
                    return;
                }
            }
            catch { }
            MessageBox.Show("WPS Office Uninstaller Tool is already running.", "Already running", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void RunApp()
        {
            Logger.LogStartup();
            Updater.CleanupOldStaging();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                Logger.Error("Unhandled UI exception", e.Exception);
                MessageBox.Show("An unexpected error occurred:\n" + e.Exception.Message + "\n\nDetails were saved to the log (use \"Open log\" in the main window).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Logger.Error("Unhandled exception", e.ExceptionObject as Exception);

            if (!NetFrameworkCheck.EnsureInstalled())
                return;

            Application.Run(new Form1());
        }
    }
}
