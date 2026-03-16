@echo off
setlocal EnableExtensions

for %%I in ("%~dp0..") do set "ROOT_DIR=%%~fI"
set "FFMPEG_ROOT=%ROOT_DIR%\ffmpeg.exe"
set "AUTOHOTKEY_EXE="

echo MultiTool runtime dependency installer
echo Root: %ROOT_DIR%
echo.
echo Note: MultiTool.exe is self-contained and already includes the .NET runtime.
echo This script only installs external tools used by optional features.
echo App-specific installer prerequisites are handled on demand inside MultiTool.
echo Examples: Git/Python for AUTOMATIC1111 and Open WebUI, 7-Zip and VC++ for RPCS3.
echo.

where winget >nul 2>&1
if errorlevel 1 (
    echo winget was not found on this machine.
    echo Install App Installer / winget first, then run this script again.
    exit /b 1
)

call :ensure_ffmpeg
if errorlevel 1 exit /b 1

call :ensure_autohotkey
if errorlevel 1 exit /b 1

echo.
echo All runtime dependencies are ready.
echo - MultiTool.exe: self-contained
echo - ffmpeg: available for local video recording
echo - AutoHotkey v2: available for the startup launcher script
echo - App installer prerequisites: installed only when the user selects those apps in MultiTool
exit /b 0

:ensure_ffmpeg
echo Checking ffmpeg...

if exist "%FFMPEG_ROOT%" (
    echo ffmpeg.exe already exists next to MultiTool.exe.
    goto :eof
)

where ffmpeg >nul 2>&1
if not errorlevel 1 (
    echo ffmpeg is already available on PATH.
    goto :eof
)

echo ffmpeg is missing. Installing with winget...
winget install --id Gyan.FFmpeg --exact --accept-package-agreements --accept-source-agreements
if errorlevel 1 (
    echo Failed to install ffmpeg.
    exit /b 1
)

where ffmpeg >nul 2>&1
if errorlevel 1 (
    echo ffmpeg install finished, but ffmpeg still is not on PATH.
    echo Reopen the terminal and try again.
    exit /b 1
)

echo ffmpeg installed successfully.
goto :eof

:ensure_autohotkey
echo.
echo Checking AutoHotkey v2...

call :resolve_autohotkey
if defined AUTOHOTKEY_EXE (
    echo AutoHotkey v2 is already installed.
    echo   %AUTOHOTKEY_EXE%
    goto :eof
)

echo AutoHotkey v2 is missing. Installing with winget...
winget install --id AutoHotkey.AutoHotkey --exact --accept-package-agreements --accept-source-agreements
if errorlevel 1 (
    echo Failed to install AutoHotkey v2.
    exit /b 1
)

call :resolve_autohotkey
if not defined AUTOHOTKEY_EXE (
    echo AutoHotkey install finished, but the expected executable was not found:
    echo   AutoHotkey64.exe / AutoHotkey.exe / AutoHotkeyU64.exe
    echo If AutoHotkey installed somewhere else, add it to PATH or update the script detection paths.
    exit /b 1
)

echo AutoHotkey v2 installed successfully.
echo   %AUTOHOTKEY_EXE%
goto :eof

:resolve_autohotkey
set "AUTOHOTKEY_EXE="

for %%P in (
    "%ProgramFiles%\AutoHotkey\v2\AutoHotkey64.exe"
    "%ProgramFiles%\AutoHotkey\AutoHotkey64.exe"
    "%ProgramFiles%\AutoHotkey\AutoHotkey.exe"
    "%ProgramFiles(x86)%\AutoHotkey\v2\AutoHotkey64.exe"
    "%ProgramFiles(x86)%\AutoHotkey\AutoHotkeyU64.exe"
    "%ProgramFiles(x86)%\AutoHotkey\AutoHotkey.exe"
) do (
    if not defined AUTOHOTKEY_EXE if exist %%~P (
        set "AUTOHOTKEY_EXE=%%~fP"
    )
)

if defined AUTOHOTKEY_EXE goto :eof

for %%E in (AutoHotkey64.exe AutoHotkey.exe AutoHotkeyU64.exe) do (
    if not defined AUTOHOTKEY_EXE (
        for /f "delims=" %%F in ('where %%E 2^>nul') do (
            if not defined AUTOHOTKEY_EXE set "AUTOHOTKEY_EXE=%%~fF"
        )
    )
)

goto :eof
