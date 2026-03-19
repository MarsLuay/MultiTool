@echo off
setlocal EnableExtensions

for %%I in ("%~dp0..") do set "ROOT_DIR=%%~fI"
set "FFMPEG_ROOT=%ROOT_DIR%\ffmpeg.exe"
set "DOTNET_EXE=%USERPROFILE%\.dotnet\dotnet.exe"
set "PROGRAM_FILES_DOTNET=%ProgramFiles%\dotnet\dotnet.exe"
set "DOTNET_SDK_WINGET_ID=Microsoft.DotNet.SDK.8"
set "WINGET_BOOTSTRAP_URL=https://aka.ms/getwinget"

echo MultiTool runtime dependency installer
echo Root: %ROOT_DIR%
echo.
echo Note: MultiTool.exe is self-contained and already includes the .NET runtime.
echo This script installs build/runtime dependencies used by setup and optional features.
echo App-specific installer prerequisites are handled on demand inside MultiTool.
echo Examples: Git/Python for AUTOMATIC1111 and Open WebUI, 7-Zip and VC++ for RPCS3.
echo.

call :ensure_winget
if errorlevel 1 exit /b 1

call :ensure_ffmpeg
if errorlevel 1 exit /b 1

call :ensure_dotnet_sdk
if errorlevel 1 exit /b 1

echo.
echo All runtime dependencies are ready.
echo - MultiTool.exe: self-contained
echo - ffmpeg: available for local video recording
echo - .NET 8 SDK: available for rebuilding/publishing MultiTool from source
echo - App installer prerequisites: installed only when the user selects those apps in MultiTool
exit /b 0

:ensure_winget
where winget >nul 2>&1
if not errorlevel 1 (
    echo winget is already installed.
    goto :eof
)

echo winget was not found. Installing App Installer / winget...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$ErrorActionPreference = 'Stop'; " ^
    "$tmpRoot = Join-Path $env:TEMP 'MultiTool-winget'; " ^
    "New-Item -ItemType Directory -Force -Path $tmpRoot | Out-Null; " ^
    "$bundlePath = Join-Path $tmpRoot 'Microsoft.DesktopAppInstaller.msixbundle'; " ^
    "Invoke-WebRequest -Uri '%WINGET_BOOTSTRAP_URL%' -OutFile $bundlePath; " ^
    "Add-AppxPackage -Path $bundlePath -ForceUpdateFromAnyVersion"
if errorlevel 1 (
    echo Failed to install App Installer / winget automatically.
    echo Install App Installer manually from Microsoft Store, then run this script again.
    exit /b 1
)

where winget >nul 2>&1
if errorlevel 1 (
    echo App Installer install finished, but winget is still unavailable in this terminal.
    echo Close and reopen the terminal, then run this script again.
    exit /b 1
)

echo App Installer / winget installed successfully.
goto :eof

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

:ensure_dotnet_sdk
echo.
echo Checking .NET 8 SDK...

if exist "%DOTNET_EXE%" (
    echo .NET SDK is already installed.
    echo   %DOTNET_EXE%
    goto :eof
)

if exist "%PROGRAM_FILES_DOTNET%" (
    echo .NET SDK is already installed.
    echo   %PROGRAM_FILES_DOTNET%
    goto :eof
)

where dotnet >nul 2>&1
if not errorlevel 1 (
    echo .NET SDK is already available on PATH.
    goto :eof
)

echo .NET SDK was not found. Installing .NET 8 SDK with winget...
winget install --id %DOTNET_SDK_WINGET_ID% --exact --accept-package-agreements --accept-source-agreements
if errorlevel 1 (
    echo Failed to install .NET 8 SDK.
    exit /b 1
)

if exist "%PROGRAM_FILES_DOTNET%" (
    echo .NET 8 SDK installed successfully.
    goto :eof
)

where dotnet >nul 2>&1
if not errorlevel 1 (
    echo .NET 8 SDK installed successfully.
    goto :eof
)

echo .NET 8 SDK install completed, but dotnet still was not found.
echo Reopen the terminal and run this script again.
exit /b 1
