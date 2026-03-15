@echo off
setlocal EnableExtensions

set "STARTUP_DIR=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup"
set "SHORTCUT_PATH=%STARTUP_DIR%\Launch MultiTool.lnk"
set "LEGACY_SHORTCUT_PATH=%STARTUP_DIR%\Launch AutoClicker.lnk"
set "REMOVED_ANY="

if exist "%SHORTCUT_PATH%" (
    del /q "%SHORTCUT_PATH%"
    if errorlevel 1 (
        echo Could not remove the Startup shortcut:
        echo   %SHORTCUT_PATH%
        exit /b 1
    )

    echo Startup shortcut removed:
    echo   %SHORTCUT_PATH%
    set "REMOVED_ANY=1"
)

if exist "%LEGACY_SHORTCUT_PATH%" (
    del /q "%LEGACY_SHORTCUT_PATH%"
    if errorlevel 1 (
        echo Could not remove the legacy Startup shortcut:
        echo   %LEGACY_SHORTCUT_PATH%
        exit /b 1
    )

    echo Legacy Startup shortcut removed:
    echo   %LEGACY_SHORTCUT_PATH%
    set "REMOVED_ANY=1"
)

if defined REMOVED_ANY (
    exit /b 0
)

echo Startup shortcut was not present:
echo   %SHORTCUT_PATH%
exit /b 0
