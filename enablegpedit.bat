@echo off
setlocal EnableExtensions
:: Usage: enablegpedit.bat [/silent]
:: Exit codes: 0 = gpedit.msc available, 1 = error, 2 = restart needed / not visible yet
set "SILENT=0"
if /i "%~1"=="/silent" set "SILENT=1"

ECHO ===================================================
ECHO Enabling Local Group Policy Editor (gpedit.msc)
ECHO NOTE: This file must be run with Admin privileges.
ECHO ===================================================

net session >nul 2>&1 && goto :admin_ok
ECHO ERROR: Please run as Administrator.
if "%SILENT%"=="0" pause
exit /b 1
:admin_ok

:: Pro / Enterprise / Education already ship gpedit.msc - nothing to do.
if exist "%SystemRoot%\System32\gpedit.msc" (
    ECHO gpedit.msc is already available. Nothing to do.
    set "RC=0"
    goto :end
)

set "FOUND=0"
set "FAILED=0"
for %%P in (ClientTools ClientExtensions) do (
    for %%f in ("%SystemRoot%\servicing\Packages\Microsoft-Windows-GroupPolicy-%%P-Package~*.mum") do (
        set "FOUND=1"
        ECHO Installing %%~nxf ...
        DISM /Online /NoRestart /Add-Package:"%%f"
        if errorlevel 1 if not errorlevel 3010 set "FAILED=1"
    )
)

if "%FOUND%"=="0" (
    ECHO ERROR: Group Policy packages not found. This Windows edition may not support gpedit.msc.
    set "RC=1"
    goto :end
)

if exist "%SystemRoot%\System32\gpedit.msc" (
    ECHO gpedit.msc enabled successfully. Run it with: Win+R ^> gpedit.msc
    set "RC=0"
) else if "%FAILED%"=="1" (
    ECHO DISM reported an error. Check C:\Windows\Logs\DISM\dism.log
    set "RC=1"
) else (
    ECHO gpedit.msc not visible yet. Please restart the computer and try again.
    set "RC=2"
)

:end
if "%SILENT%"=="0" pause
exit /b %RC%
