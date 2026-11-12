# 🗑️ WPS Office Uninstaller Tool

[![Latest release](https://img.shields.io/github/v/release/ThanhNguyenVN93/WPS-office-uninstaller-tool)](https://github.com/ThanhNguyenVN93/WPS-office-uninstaller-tool/releases/latest)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A small Windows utility that removes **WPS Office** completely: it runs the official uninstaller, deletes the residual files and registry leftovers, gives the Office file types back to **Microsoft Office**, and (optionally) blocks WPS from being installed again.

It is handy when WPS keeps coming back, when a reinstall fails because of leftovers, when Word/Excel/PowerPoint files show broken icons or still try to open with WPS after it was removed, or when you want a clean system.

## ⬇️ Download

**[Download the latest `WPS.OFFICE.UNINSTALLER.TOOL.exe`](https://github.com/ThanhNguyenVN93/WPS-office-uninstaller-tool/releases/latest/download/WPS.OFFICE.UNINSTALLER.TOOL.exe)**
(all versions and release notes: [Releases](https://github.com/ThanhNguyenVN93/WPS-office-uninstaller-tool/releases))

## 💻 How to use

1. Run the exe (UAC will ask for administrator rights when it is needed).
2. **UNINSTALL** – starts the WPS uninstaller found on the PC.
3. **CLEAN UP!** – in this order:
   1. deletes the leftover data (if some files are locked by Windows Explorer through the WPS shell extension, the tool offers to restart Explorer to release them);
   2. restores the file associations and removes the WPS registry leftovers (see [below](#-file-associations-and-leftovers-v160));
   3. applies the reinstall block;
   4. enables `gpedit.msc` (Windows Home).

   Steps 2–4 each ask for administrator rights (UAC). A step-by-step result is shown at the end.
4. **Unblock WPS** (link at the bottom) – removes the reinstall block whenever you want to install WPS again. UNINSTALL also lifts the block automatically.
5. **Open log** (link at the bottom) – opens the log folder; attach the latest log when you report a bug.

## ✨ Features

- Runs the WPS uninstaller (per-user and Program Files installs, including the older `Kingsoft\WPS Office` layout).
- Cleans residual data: `%LocalAppData%\Kingsoft`, `%AppData%\kingsoft`, `%ProgramData%\kingsoft`, `%ProgramData%\WPS` and any leftover `Program Files\WPS Office`. Your **WPS Cloud document folders are never touched**.
- Handles read-only files and files locked by Explorer.
- **Restores file associations (v1.6.0+)**: with Microsoft Office installed, Word/Excel/PowerPoint files open with Office again and get their icons back. Without Office, WPS's associations are removed. See [File associations and leftovers](#-file-associations-and-leftovers-v160).
- **Removes WPS leftovers (v1.6.0+)** the uninstaller keeps: scheduled tasks, autostart entries, the "WPS Cloud" item in Explorer, context-menu handlers / overlay icons, services and orphaned "Programs and Features" entries.
- **Blocks reinstalling WPS** (layers, see below).
- Enables the Local Group Policy Editor (`gpedit.msc`) on Windows Home.
- Result summary after CLEAN UP, single running instance, local log file.
- **Release notes in the app (v1.5.1+)**: the "Update available" window shows the release's *What's new*, and the first start after an update shows the notes of the installed version once.
- **Auto-update (v1.5.0+)**: when a new release exists the tool asks whether to update. *Update now* downloads the exe from this repository's GitHub Releases, verifies its SHA-256 against the release's `SHA256SUMS.txt` and its version number, replaces the installed exe and restarts. If anything does not match, nothing is replaced. Versions before 1.5.0 only show a notice and open the download page, so download v1.5.0 or newer manually once.

## 📂 File associations and leftovers (v1.6.0+)

WPS takes over the Office file types while it is installed, and its uninstaller often leaves them pointing to programs that no longer exist. That is why files show blank icons or open nothing afterwards. CLEAN UP fixes this for `.doc/.docx/.docm/.dot/.dotx/.dotm/.rtf/.odt`, `.xls/.xlsx/.xlsm/.xlsb/.xlt/.xltx/.xltm/.csv/.ods` and `.ppt/.pptx/.pptm/.pps/.ppsx/.ppsm/.pot/.potx/.potm/.odp`:

| Situation | What the tool does |
|---|---|
| Microsoft Office is installed | Each type points to the Office program again (icons and double-click come back). |
| Microsoft Office is not installed | WPS's association is removed and Windows is back to its own state: the first time you open such a file, Windows asks which app to use. |
| `.pdf`, `.txt`, `.xml` | Only changed when they really point to WPS. Associations of other apps are left alone. |
| Your own "Always use this app" choice points to WPS | That choice is deleted, so Windows falls back to the restored association. |

What else it removes:
- WPS's "New > WPS document" menu entries and the WPS program registrations.
- Scheduled tasks, autostart entries and services that run WPS programs.
- The "WPS Cloud" item in Explorer's navigation pane.
- WPS context-menu handlers and overlay icons.
- "Programs and Features" entries whose WPS uninstaller is already gone.

Leftovers are matched by their program path (`Kingsoft` / WPS executables), not by loose names.

Fallbacks:
- **UAC declined:** your own user account is still fixed (Office types are set per user).
- **Some types still wrong with Office Click-to-Run:** the tool offers to run **Office Quick Repair**. It takes a few minutes, needs no internet, and closes the Office apps.
- **MSI Office:** repair it from *Settings > Apps > Microsoft Office > Modify*.
- **No Office:** use *Open with > Always* once for that type.

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
- On Windows 11, Windows itself may lock the `.pdf` choice so it cannot be removed. The log shows a warning; pick the app once with *Open with > Always*.
- The leftover cleanup removes anything that runs a program from a `Kingsoft` folder, so it also affects other Kingsoft products.
- Uninstall, block and cleanup were tested on Windows 10 Pro (not yet on Windows Home). The v1.6.0 association restore was tested on Windows 11 Pro with Office Click-to-Run, using simulated WPS leftovers rather than a real WPS install.
- Back up important WPS documents before running the tool. It modifies system folders and the registry; use it only for WPS Office.

## 🧰 Requirements

- Windows 10/11 with **.NET Framework 4.8** (the tool checks and guides you if it is missing).
- Administrator rights for the uninstall, file-association, block and `gpedit.msc` steps.

## 🔨 Build from source

```bash
msbuild frmwpsoffice.csproj /p:Configuration=Release
```

Visual Studio 2022 or Build Tools with the .NET Framework 4.8 targeting pack is required. GitHub Actions builds every push and pull request.

### Releasing

Releases are built and published by GitHub Actions (`.github/workflows/release.yml`):

1. Set `AssemblyVersion` and `AssemblyFileVersion` in `Properties/AssemblyInfo.cs` to `X.Y.Z.0`.
2. Write the release notes in `release-notes/vX.Y.Z.md`. They are shown on the release page and in the app's *What's new* window.
3. Merge to `master`, then push the tag:

```bash
git tag vX.Y.Z origin/master && git push origin vX.Y.Z
```

The workflow then:
- checks that the version and the notes match the tag;
- builds the exe and creates `SHA256SUMS.txt` for the auto-updater;
- publishes the release with the exe, `SHA256SUMS.txt` and `README.md`.

To test without publishing, run the workflow by hand (*Actions > Release > Run workflow*). It runs the same build and checks, and keeps the files as a workflow artifact.

## 🤝 Support and contributing

- Found a bug? Open an [Issue](https://github.com/ThanhNguyenVN93/WPS-office-uninstaller-tool/issues) and attach the log from **Open log**.
- Pull requests are welcome, for example for the cleanup routines or new features.

## 📄 License

Released under the [MIT License](LICENSE).
