@echo off
setlocal EnableExtensions

for %%I in ("%~dp0..") do set "ROOT_DIR=%%~fI"
set "APP_PATH=%ROOT_DIR%\MultiTool.exe"
set "RUN_KEY=HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
set "RUN_VALUE_NAME=MultiTool"
set "STARTUP_DIR=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup"
set "SHORTCUT_PATH=%STARTUP_DIR%\Launch MultiTool.lnk"
set "LEGACY_SHORTCUT_PATH=%STARTUP_DIR%\Launch AutoClicker.lnk"

if not exist "%APP_PATH%" (
    echo Could not find MultiTool.exe:
    echo   %APP_PATH%
    exit /b 1
)

if not exist "%STARTUP_DIR%" (
    mkdir "%STARTUP_DIR%"
)

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$runPath = $env:RUN_KEY; " ^
    "New-Item -Path $runPath -Force | Out-Null; " ^
    "$appPath = $env:APP_PATH; " ^
    "$value = [string]::Concat([char]34, $appPath, [char]34, ' --startup-launch'); " ^
    "Set-ItemProperty -Path $runPath -Name $env:RUN_VALUE_NAME -Value $value"
if errorlevel 1 (
    echo Could not enable Run at startup for MultiTool.
    exit /b 1
)

call :remove_legacy_shortcuts
if errorlevel 1 (
    exit /b 1
)

echo Run at startup is now enabled for MultiTool.
echo You can change this later inside MultiTool Settings using Run at startup.
exit /b 0

:remove_legacy_shortcuts
for %%P in ("%SHORTCUT_PATH%" "%LEGACY_SHORTCUT_PATH%") do (
    if exist "%%~fP" (
        del /q "%%~fP"
        if errorlevel 1 (
            echo Could not remove the legacy Startup shortcut:
            echo   %%~fP
            exit /b 1
        )

        echo Removed legacy Startup shortcut:
        echo   %%~fP
    )
)
goto :eof
