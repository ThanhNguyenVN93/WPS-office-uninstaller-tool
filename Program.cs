using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace frmwpsoffice
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Logger.LogStartup();
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
