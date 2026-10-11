@echo off
title Go Bo CadCoreFramework v2.0 khoi AutoCAD
cls

echo ======================================================================
echo    GO BO CadCoreFramework v2.0 KHOI AUTOCAD
echo ======================================================================
echo.

set "BUNDLE_DEST=%APPDATA%\Autodesk\ApplicationPlugins\CadCoreFramework.bundle"

if exist "%BUNDLE_DEST%" (
    echo Dang xoa goi CadCoreFramework.bundle...
    rmdir /s /q "%BUNDLE_DEST%" >nul 2>&1
    if not exist "%BUNDLE_DEST%" (
        echo.
        echo ======================================================================
        echo   DA GO BO CadCoreFramework HOAN TOAN KHOI AUTOCAD!
        echo ======================================================================
    ) else (
        echo [LOI] Khong the xoa thu muc. Vui long tat AutoCAD va thu lai!
    )
) else (
    echo [THONG BAO] Khong tim thay plugin tai:
    echo %BUNDLE_DEST%
)

echo.
pause
