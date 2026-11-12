# 🗑️ WPS Office Uninstaller Tool

[![Latest release](https://img.shields.io/github/v/release/ThanhNguyenVN93/WPS-office-uninstaller-tool)](https://github.com/ThanhNguyenVN93/WPS-office-uninstaller-tool/releases/latest)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A small Windows utility that removes **WPS Office** completely: it runs the official uninstaller, deletes the residual files that are left behind, and (optionally) blocks WPS from being installed again.

It is handy when WPS keeps coming back, when a reinstall fails because of leftovers, or when you want a clean system.

## ⬇️ Download

**[Download the latest `WPS.OFFICE.UNINSTALLER.TOOL.exe`](https://github.com/ThanhNguyenVN93/WPS-office-uninstaller-tool/releases/latest/download/WPS.OFFICE.UNINSTALLER.TOOL.exe)**
(all versions and release notes: [Releases](https://github.com/ThanhNguyenVN93/WPS-office-uninstaller-tool/releases))

## 💻 How to use

1. Run the exe (UAC will ask for administrator rights when it is needed).
2. **UNINSTALL** – starts the WPS uninstaller found on the PC.
3. **CLEAN UP!** – deletes the leftover data, applies the reinstall block and enables `gpedit.msc` (Windows Home). A step-by-step result is shown at the end. If some files are locked by Windows Explorer (WPS shell extension), the tool offers to restart Explorer to release them.
4. **Unblock WPS** (link at the bottom) – removes the reinstall block whenever you want to install WPS again. UNINSTALL also lifts the block automatically.
5. **Open log** (link at the bottom) – opens the log folder; attach the latest log when you report a bug.

## ✨ Features

- Runs the WPS uninstaller (per-user and Program Files installs, including the older `Kingsoft\WPS Office` layout).
- Cleans residual data: `%LocalAppData%\Kingsoft`, `%AppData%\kingsoft`, `%ProgramData%\kingsoft`, `%ProgramData%\WPS` and any leftover `Program Files\WPS Office`. Your **WPS Cloud document folders are never touched**.
- Handles read-only files and files locked by Explorer.
- **Restores file associations** (CLEAN UP): if Microsoft Office is installed, `.doc/.docx/.xls/.xlsx/.ppt/.pptx/...` point to Office again (icons + double-click). Without Office, WPS's associations are removed so Windows is back to its own state. Fallback when something is still wrong: Office Quick Repair (Click-to-Run), or a per-user fix when admin permission is denied.
- **Removes WPS leftovers** the uninstaller keeps: scheduled tasks, autostart entries, the "WPS Cloud" item in Explorer, WPS context-menu handlers / overlay icons, WPS services and orphaned "Programs and Features" entries (matched by their Kingsoft/WPS program path).
- **Blocks reinstalling WPS** (layers, see below).
- Enables the Local Group Policy Editor (`gpedit.msc`) on Windows Home.
- Result summary after CLEAN UP, single running instance, local log file.
- **Release notes in the app (v1.5.1+)**: the "Update available" window shows the release's *What's new*, and the first start after an update shows the notes of the installed version once.
- **Auto-update (v1.5.0+)**: when a new release exists the tool asks whether to update. *Update now* downloads the exe from this repository's GitHub Releases, verifies its SHA-256 against the release's `SHA256SUMS.txt` and its version number, replaces the installed exe and restarts. If anything does not match, nothing is replaced. Versions before 1.5.0 only show a notice and open the download page, so download v1.5.0 or newer manually once.

## 🔒 How the reinstall block works

`blockwps.bat` (embedded in the exe) applies several layers:

| Layer | What it does | Notes |
|---|---|---|
| Install-path lock (main layer) | Replaces the folders WPS installs into with a read-only placeholder **file**: `%LocalAppData%\Kingsoft`, `%AppData%\kingsoft`, `%ProgramData%\kingsoft`, `Program Files (x86)\WPS Office`, `Program Files\WPS Office`, `Program Files[ (x86)]\Kingsoft` | Works for any installer file name. The installer gets stuck (around 60–90 %) instead of installing. |
| Explorer policy (`DisallowRun`) | Stops WPS programs from starting from Explorer | Current user only |
| Image File Execution Options | Makes `wps.exe`, `wpp.exe`, `et.exe`, `wpspdf.exe`, `ksolaunch.exe`, … exit immediately | Machine-wide, matches by file name |
| Software Restriction Policy | Path rules such as `*\wps_wid*.exe` | Extra layer; did **not** take effect on the Windows 10 Pro test machine |

To remove everything use the **Unblock WPS** link.

## ⚠️ Limitations and warnings

- The block follows the default install locations. An installer that lets the user pick another folder (for example `D:\WPS`) can still install.
- It is not tamper-proof: anyone with administrator rights can unblock.
- IFEO blocks any program with those exact names, including non-WPS software named `et.exe` or `wpp.exe`, while the block is active.
- `%ProgramData%\kingsoft` is locked too, so other Kingsoft products cannot write there while the block is active.
- The exe is not code-signed, so Windows SmartScreen or an antivirus may warn because the tool edits the registry, stops processes and restarts Explorer. The self-update (download + replace itself) can trigger the same warnings.
- Auto-update trusts this repository's GitHub releases: whoever can publish a release can ship an exe, and the checksum file lives in the same release. Keep two-factor authentication on the GitHub account.
- Tested on Windows 10 Pro only (not yet on Windows Home / Windows 11).
- Back up important WPS documents before running the tool. It modifies system folders and the registry; use it only for WPS Office.

## 🧰 Requirements

- Windows 10/11 with **.NET Framework 4.8** (the tool checks and guides you if it is missing).
- Administrator rights for the uninstall, block and `gpedit.msc` steps.

## 🔨 Build from source

```bash
msbuild frmwpsoffice.csproj /p:Configuration=Release
```

Visual Studio 2022 or Build Tools with the .NET Framework 4.8 targeting pack is required. GitHub Actions builds every push and pull request.

## 🤝 Support and contributing

- Found a bug? Open an [Issue](https://github.com/ThanhNguyenVN93/WPS-office-uninstaller-tool/issues) and attach the log from **Open log**.
- Pull requests are welcome, for example for the cleanup routines or new features.

## 📄 License

Released under the [MIT License](LICENSE).
