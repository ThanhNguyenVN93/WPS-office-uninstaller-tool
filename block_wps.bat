@echo off
ECHO ===================================================
ECHO Kich hoat chuc nang chan ung dung WPS qua Registry
ECHO LUU Y: Phai chay file nay voi quyen Admin!
ECHO ===================================================

:: 1. Kich hoat tinh nang "Don't run specific windows applications" (DisallowRun = 1)
:: Policy duoc luu tai: HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer
REG ADD "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer" /v "DisallowRun" /t REG_DWORD /d 1 /f

:: 2. Tao khoa con de liet ke cac ung dung bi chan
REG ADD "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\DisallowRun" /f

:: 3. Them cac file .exe cua WPS vao danh sach chan (Su dung so thu tu: 1, 2, 3...)

:: Chan file cai dat
REG ADD "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\DisallowRun" /v 1 /t REG_SZ /d "wps_office_inst.exe" /f

:: Chan tien trinh chay nen/thong bao
REG ADD "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\DisallowRun" /v 2 /t REG_SZ /d "wpscenter.exe" /f

ECHO.
ECHO Da them cac ung dung WPS Office vao danh sach chan Registry.
ECHO Nguoi dung phai dang xuat/dang nhap lai hoac khoi dong lai may de thay doi co hieu luc.
ECHO.
pause