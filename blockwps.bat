@echo off
ECHO ===================================================
ECHO Activating WPS application blocking via Registry
ECHO NOTE: This file must be run with Admin privileges!
ECHO ===================================================

:: 1. Enable "Don't run specific windows applications" feature (DisallowRun = 1)
:: Policy is stored at: HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer
REG ADD "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer" /v "DisallowRun" /t REG_DWORD /d 1 /f

:: 2. Create a subkey to list the blocked applications
REG ADD "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\DisallowRun" /f

:: 3. Add WPS .exe files to the block list (Using sequential numbers: 1, 2, 3...)

:: Block the installer file
REG ADD "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\DisallowRun" /v 1 /t REG_SZ /d "wps_office_inst.exe" /f

:: Block the background/notification process
REG ADD "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\DisallowRun" /v 2 /t REG_SZ /d "wpscenter.exe" /f

ECHO.
ECHO WPS Office applications have been added to the Registry block list.
ECHO You must log out/log back in or restart the computer for the changes to take effect.
ECHO.
pause