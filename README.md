🗑️ WPS Office Uninstaller Tool
Project Overview
A lightweight yet powerful utility designed to completely remove all versions of WPS Office, including lingering files, registry keys, and residual components often left behind by the standard uninstallation process.

This tool is especially useful if you are experiencing errors when trying to reinstall WPS Office or if you want to ensure a clean slate on your system.

✨ Key Features: Support users to uninstall WPS Office

- Clean up WPS Office residual files (Local/Roaming data)
- Block WPS reinstall: per-user Explorer policy (`DisallowRun`) + machine-wide Image File Execution Options block for the installer and WPS processes + Software Restriction Policy path rules with wildcards (catches installers with any file name; Pro/Enterprise) + install-folder lock (deny creating files in `%LocalAppData%\Kingsoft` and `Program Files\WPS Office`, works for any installer name) (`blockwps.bat`; undo with `blockwps.bat /remove`)
- Auto-enable Local Group Policy Editor (`gpedit.msc`) on Windows Home (`enablegpedit.bat`)
- "Unblock WPS" link removes the reinstall block at any time (UNINSTALL also lifts it automatically); CLEAN UP also removes `%ProgramData%\kingsoft` and `%ProgramData%\WPS` (WPS Cloud document folders are never touched)
- Local log file for bug reports: `%AppData%\WPSOfficeUninstaller\logspp-YYYYMMDD.log` ("Open log" link in the window; attach it to GitHub issues)
- Optional web donate page (needs WebView2 Runtime + `Microsoft.Web.WebView2.Core.dll`, `Microsoft.Web.WebView2.WinForms.dll`, `WebView2Loader.dll` next to the exe; otherwise the built-in QR form is used)
- Built-in update check against GitHub Releases (click the version link at the bottom of the window)

🛠️ System Requirements: Any windows which has wps office in the system.

Permissions: Administrator privileges are required to perform the necessary uninstallation and Registry modifications.

💻 How to Use (Installation & Execution): Download and run the Tool (pre-built exe is in the `dist/` folder and on the Releases page)

⚠️ Warnings and Important Notes
Use with Caution: This tool makes direct modifications to system folders and the Windows Registry. Please use it responsibly and as intended.

Data Backup: While the tool only targets the application files, it is always best practice to back up any important WPS documents before running the uninstaller.

WPS Only: This utility is designed only to remove WPS Office. Do not attempt to use it on other applications.

🤝 Support and Contributing
If you encounter a bug or have suggestions for improvement:

Report Bugs: Please open a new Issue in the Issues tab on GitHub.

Contributing Code: Pull Requests (PRs) are welcome if you wish to enhance the cleanup routines or add features!

📄 License
This tool is released under the MIT License.
