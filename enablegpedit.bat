@echo off
setlocal
ECHO ===================================================
ECHO Enabling Local Group Policy Editor (gpedit.msc)
ECHO NOTE: This file must be run with Admin privileges!
ECHO ===================================================

net session >nul 2>&1 || (ECHO ERROR: Please run as Administrator.& pause & exit /b 1)

:: Pro / Enterprise / Education already ship gpedit.msc - nothing to do.
if exist "%SystemRoot%\System32\gpedit.msc" (
    ECHO gpedit.msc is already available. Nothing to do.
    goto :done
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
    pause & exit /b 1
)

:done
if exist "%SystemRoot%\System32\gpedit.msc" (
    ECHO.
    ECHO gpedit.msc enabled successfully. Run it with: Win+R ^> gpedit.msc
) else if "%FAILED%"=="1" (
    ECHO.
    ECHO DISM reported an error. Check C:\Windows\Logs\DISM\dism.log
) else (
    ECHO.
    ECHO gpedit.msc not visible yet. Please restart the computer and try again.
)
pause
endlocal
