@echo off
setlocal
title Cai Dat CadCoreFramework v2.0 cho AutoCAD
cls

echo ======================================================================
echo    CAI DAT CadCoreFramework v2.0 - PLUGIN QUAN LY THIET BI DIEN AUTOCAD
echo ======================================================================
echo.

set "PLUGIN_DIR=%APPDATA%\Autodesk\ApplicationPlugins"
set "BUNDLE_SRC=%~dp0CadCoreFramework.bundle"
set "BUNDLE_DEST=%PLUGIN_DIR%\CadCoreFramework.bundle"

:: Neu khong tim thay o thu muc hien tai, kiem tra thu muc dist Release
if not exist "%BUNDLE_SRC%" (
    if exist "%~dp0..\dist\CadCoreFramework_Release\CadCoreFramework.bundle" (
        set "BUNDLE_SRC=%~dp0..\dist\CadCoreFramework_Release\CadCoreFramework.bundle"
    )
)

if not exist "%BUNDLE_SRC%" goto ERROR_MISSING

echo [1/3] Kiem tra thu muc ApplicationPlugins...
if not exist "%PLUGIN_DIR%" mkdir "%PLUGIN_DIR%"

echo [2/3] Xoa ban cai dat cu neu co...
if exist "%BUNDLE_DEST%" rmdir /s /q "%BUNDLE_DEST%" >nul 2>&1

echo [3/3] Sao chep plugin vao AutoCAD ApplicationPlugins...
xcopy "%BUNDLE_SRC%" "%BUNDLE_DEST%\" /E /I /Y /Q >nul

if exist "%BUNDLE_DEST%\PackageContents.xml" goto SUCCESS
goto ERROR_COPY

:SUCCESS
echo.
echo ======================================================================
echo   CAI DAT HOAN TAT VA THANH CONG 100%%!
echo ======================================================================
echo Vi tri cai dat:
echo   %BUNDLE_DEST%
echo.
echo HUONG DAN SU DUNG:
echo 1. Khoi dong AutoCAD (phien ban 2021 den 2025).
echo 2. Plugin se tu dong duoc AutoCAD nap qua Autoloader.
echo 3. Go cac lenh sau tren thanh Command Line cua AutoCAD:
echo    - CORE_INFO    : Kiem tra trang thai khoi tao cua plugin
echo    - SHOW_PALETTE   : Mo bang Palette cong cu
echo ======================================================================
echo.
echo Thong bao: Cua so nay se tu dong dong lai sau khi ban nhan phim bat ky.
goto FINISH

:ERROR_MISSING
echo [LOI] Khong tim thay thu muc: "%BUNDLE_SRC%"!
echo.
echo NGUYEN NHAN:
echo Ban co the dang mo truc tiep file tu trong file nen ZIP ma chua giai nen.
echo.
echo HUONG DAN KHAC PHUC:
echo 1. Nhan chuot phai vao file CadCoreFramework_v2.0_Setup.zip.
echo 2. Chon Extract All de giai nen toan bo ra thu muc roi moi chay Install.bat.
goto FINISH

:ERROR_COPY
echo.
echo [LOI] Khong the sao chep vao %BUNDLE_DEST%!
echo Vui long tat AutoCAD neu dang chay va thu lai voi quyen Administrator.
goto FINISH

:FINISH
echo.
pause
exit /b 0
