@echo off
setlocal EnableExtensions

for %%I in ("%~dp0..") do set "ROOT_DIR=%%~fI"
set "SCRIPT_PATH=%ROOT_DIR%\tools\startup\LaunchMultiTool.ahk"
set "APP_PATH=%ROOT_DIR%\MultiTool.exe"
set "STARTUP_DIR=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup"
set "SHORTCUT_PATH=%STARTUP_DIR%\Launch MultiTool.lnk"
set "LEGACY_SHORTCUT_PATH=%STARTUP_DIR%\Launch AutoClicker.lnk"
set "AHK_EXE="

if not exist "%SCRIPT_PATH%" (
    echo Could not find the AutoHotkey script:
    echo   %SCRIPT_PATH%
    exit /b 1
)

if not exist "%APP_PATH%" (
    echo Could not find MultiTool.exe:
    echo   %APP_PATH%
    exit /b 1
)

if not exist "%STARTUP_DIR%" (
    mkdir "%STARTUP_DIR%"
)

call :resolve_autohotkey

if defined AHK_EXE (
    powershell -NoProfile -ExecutionPolicy Bypass -Command ^
        "$ws = New-Object -ComObject WScript.Shell; " ^
        "$shortcut = $ws.CreateShortcut('%SHORTCUT_PATH%'); " ^
        "$shortcut.TargetPath = '%AHK_EXE%'; " ^
        "$shortcut.Arguments = '\"%SCRIPT_PATH%\"'; " ^
        "$shortcut.WorkingDirectory = '%ROOT_DIR%'; " ^
        "$shortcut.IconLocation = '%APP_PATH%,0'; " ^
        "$shortcut.Save()"
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -Command ^
        "$ws = New-Object -ComObject WScript.Shell; " ^
        "$shortcut = $ws.CreateShortcut('%SHORTCUT_PATH%'); " ^
        "$shortcut.TargetPath = '%SCRIPT_PATH%'; " ^
        "$shortcut.WorkingDirectory = '%ROOT_DIR%'; " ^
        "$shortcut.IconLocation = '%APP_PATH%,0'; " ^
        "$shortcut.Save()"
    echo AutoHotkey executable was not found in the default install paths.
    echo Created a Startup shortcut directly to the .ahk script instead.
    echo That requires .ahk files to be associated with AutoHotkey on this PC.
)

if exist "%LEGACY_SHORTCUT_PATH%" (
    del /q "%LEGACY_SHORTCUT_PATH%"
)

echo Startup shortcut created:
echo   %SHORTCUT_PATH%
exit /b 0

:resolve_autohotkey
set "AHK_EXE="

for %%P in (
    "%ProgramFiles%\AutoHotkey\v2\AutoHotkey64.exe"
    "%ProgramFiles%\AutoHotkey\AutoHotkey64.exe"
    "%ProgramFiles%\AutoHotkey\AutoHotkey.exe"
    "%ProgramFiles(x86)%\AutoHotkey\v2\AutoHotkey64.exe"
    "%ProgramFiles(x86)%\AutoHotkey\AutoHotkeyU64.exe"
    "%ProgramFiles(x86)%\AutoHotkey\AutoHotkey.exe"
) do (
    if not defined AHK_EXE if exist %%~P (
        set "AHK_EXE=%%~fP"
    )
)

if defined AHK_EXE goto :eof

for %%E in (AutoHotkey64.exe AutoHotkey.exe AutoHotkeyU64.exe) do (
    if not defined AHK_EXE (
        for /f "delims=" %%F in ('where %%E 2^>nul') do (
            if not defined AHK_EXE set "AHK_EXE=%%~fF"
        )
    )
)

goto :eof
