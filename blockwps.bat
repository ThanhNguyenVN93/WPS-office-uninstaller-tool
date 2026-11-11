@echo off
setlocal EnableExtensions EnableDelayedExpansion
:: Usage: blockwps.bat [/silent] [/remove] [<user SID>]
::   /silent  no pause at the end (used by the app)
::   /remove  remove the block again
::   SID      write the per-user policy into that user's hive instead of HKCU
:: Exit codes: 0 = OK, 1 = error / not admin

set "SILENT=0"
set "REMOVE=0"
set "SID="
for %%A in (%*) do (
    if /i "%%~A"=="/silent" set "SILENT=1"
    if /i "%%~A"=="/remove" set "REMOVE=1"
    echo %%~A| findstr /b /i "S-1-5-" >nul && set "SID=%%~A"
)

ECHO ===================================================
ECHO WPS Office blocking (Explorer policy + IFEO)
ECHO NOTE: This file must be run with Admin privileges.
ECHO ===================================================

net session >nul 2>&1 && goto :admin_ok
ECHO ERROR: Please run as Administrator.
if "%SILENT%"=="0" pause
exit /b 1
:admin_ok

:: Per-user policy root: the real logged-in user (when elevated by another admin) or HKCU.
set "ROOT=HKCU"
if defined SID reg query "HKU\%SID%" >nul 2>&1 && set "ROOT=HKU\%SID%"

set "POL=%ROOT%\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"
set "IFEO=HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options"
:: Installer + every WPS process that could (re)install or start it
set "APPS=wps_office_inst.exe wpscenter.exe wpscloudsvr.exe wpsupdate.exe ksolaunch.exe wps.exe et.exe wpp.exe wpspdf.exe"
set "ERR=0"

if "%REMOVE%"=="1" goto :remove

:: 1. Per-user "Don't run specified Windows applications" (Group Policy: DisallowRun)
reg add "%POL%" /v DisallowRun /t REG_DWORD /d 1 /f >nul 2>&1 || set "ERR=1"
for %%E in (%APPS%) do (
    reg add "%POL%\DisallowRun" /v "%%E" /t REG_SZ /d "%%E" /f >nul 2>&1 || set "ERR=1"
)

:: 2. Machine-wide block via Image File Execution Options (also stops cmd/PowerShell launches)
for %%E in (%APPS%) do (
    reg add "%IFEO%\%%E" /v Debugger /t REG_SZ /d "cmd.exe /c exit" /f /reg:64 >nul 2>&1 || set "ERR=1"
    reg add "%IFEO%\%%E" /v Debugger /t REG_SZ /d "cmd.exe /c exit" /f /reg:32 >nul 2>&1 || set "ERR=1"
)

:: 3. Software Restriction Policy (Group Policy: Computer Configuration > Windows Settings > Security Settings
::    > Software Restriction Policies). Path rules accept wildcards, so installers with any file name
::    (WPSOffice_12.x.exe, wps_office_*.exe ...) and anything run from the WPS folders are refused.
::    Existing SRP configuration is left untouched; only the base values are created when missing.
set "SRP=HKLM\SOFTWARE\Policies\Microsoft\Windows\Safer\CodeIdentifiers"
reg query "%SRP%" /v DefaultLevel >nul 2>&1 || (
    reg add "%SRP%" /v DefaultLevel /t REG_DWORD /d 262144 /f >nul 2>&1 || set "ERR=1"
    reg add "%SRP%" /v TransparentEnabled /t REG_DWORD /d 1 /f >nul 2>&1
    reg add "%SRP%" /v PolicyScope /t REG_DWORD /d 0 /f >nul 2>&1
    reg add "%SRP%" /v AuthenticodeEnabled /t REG_DWORD /d 0 /f >nul 2>&1
)
:: File types the policy applies to (what gpedit writes by default). Without it SRP may enforce nothing.
reg query "%SRP%" /v ExecutableTypes >nul 2>&1 || reg add "%SRP%" /v ExecutableTypes /t REG_MULTI_SZ /s "," /d "ADE,ADP,BAS,BAT,CHM,CMD,COM,CPL,CRT,EXE,HLP,HTA,INF,INS,ISP,LNK,MDB,MDE,MSC,MSI,MSP,MST,OCX,PCD,PIF,REG,SCR,SHS,URL,VB,WSC" /f >nul 2>&1
call :srp_add 01 "*\WPSOffice*.exe"
call :srp_add 02 "*\wps_office*.exe"
call :srp_add 05 "*\wps_wid*.exe"
call :srp_add 06 "*\konlinesetup*.exe"
call :srp_add 03 "*\Kingsoft\WPS Office\*"
call :srp_add 04 "*\WPS Office\*"

if "%ERR%"=="0" (
    ECHO WPS Office applications are now blocked. Restart the computer to apply all policies.
) else (
    ECHO Some registry entries could not be written.
)
goto :end

:remove
for %%E in (%APPS%) do (
    reg delete "%POL%\DisallowRun" /v "%%E" /f >nul 2>&1
    reg delete "%IFEO%\%%E" /v Debugger /f /reg:64 >nul 2>&1
    reg delete "%IFEO%\%%E" /v Debugger /f /reg:32 >nul 2>&1
)
for %%N in (01 02 03 04 05 06) do reg delete "HKLM\SOFTWARE\Policies\Microsoft\Windows\Safer\CodeIdentifiers\0\Paths\{7a1d0c00-5a11-4f00-9a00-0000000000%%N}" /f >nul 2>&1
ECHO WPS Office block removed.

:end
if "%SILENT%"=="0" pause
exit /b %ERR%

:srp_add
:: %1 = rule number (2 digits), %2 = path pattern
set "RULE=%SRP%\0\Paths\{7a1d0c00-5a11-4f00-9a00-0000000000%~1}"
:: Same value types gpedit itself writes (ItemData = REG_EXPAND_SZ, LastModified = FILETIME).
reg add "%RULE%" /v ItemData /t REG_EXPAND_SZ /d "%~2" /f >nul 2>&1 || set "ERR=1"
reg add "%RULE%" /v LastModified /t REG_QWORD /d 0x01dd71c2c5557f53 /f >nul 2>&1
reg add "%RULE%" /v SaferFlags /t REG_DWORD /d 0 /f >nul 2>&1
reg add "%RULE%" /v Description /t REG_SZ /d "Blocked by WPS Office Uninstaller Tool" /f >nul 2>&1
exit /b 0
