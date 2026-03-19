@echo off
setlocal EnableExtensions

set "RUN_KEY=HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
set "RUN_VALUE_NAME=MultiTool"
set "STARTUP_DIR=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup"
set "SHORTCUT_PATH=%STARTUP_DIR%\Launch MultiTool.lnk"
set "LEGACY_SHORTCUT_PATH=%STARTUP_DIR%\Launch AutoClicker.lnk"
set "REMOVED_ANY="

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "Remove-ItemProperty -Path $env:RUN_KEY -Name $env:RUN_VALUE_NAME -ErrorAction SilentlyContinue"
if errorlevel 1 (
    echo Could not disable Run at startup for MultiTool.
    exit /b 1
)

if exist "%SHORTCUT_PATH%" (
    del /q "%SHORTCUT_PATH%"
    if errorlevel 1 (
        echo Could not remove the legacy Startup shortcut:
        echo   %SHORTCUT_PATH%
        exit /b 1
    )

    echo Removed legacy Startup shortcut:
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

    echo Removed legacy Startup shortcut:
    echo   %LEGACY_SHORTCUT_PATH%
    set "REMOVED_ANY=1"
)

echo Run at startup is now disabled for MultiTool.
if defined REMOVED_ANY (
    echo Legacy Startup shortcuts were also removed.
    exit /b 0
)

echo No legacy Startup shortcuts were present.
exit /b 0
