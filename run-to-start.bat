@echo off
setlocal EnableExtensions

set "SELF_PATH=%~f0"
set "NO_LAUNCH="
set "RELAUNCH_ARGS=%*"

if /I "%~1"=="--elevated" shift
if /I "%~1"=="--no-launch" set "NO_LAUNCH=1"

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent()); " ^
    "if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { exit 0 } else { exit 1 }"
if errorlevel 1 (
    echo Requesting administrator access for MultiTool setup and hardware telemetry...
    powershell -NoProfile -ExecutionPolicy Bypass -Command ^
        "try { " ^
        "  $argumentLine = '/c """"' + $env:SELF_PATH + '"" --elevated ' + $env:RELAUNCH_ARGS + '"'; " ^
        "  $process = Start-Process -FilePath $env:ComSpec -ArgumentList $argumentLine -Verb RunAs -PassThru -Wait; " ^
        "  exit $process.ExitCode; " ^
        "} catch { exit 1223 }"
    exit /b %errorlevel%
)

set "ROOT_DIR=%~dp0"
set "REBUILD_SCRIPT=%ROOT_DIR%tools\rebuild-app.bat"
set "DEPENDENCY_SCRIPT=%ROOT_DIR%tools\install-runtime-dependencies.bat"
set "STARTUP_SCRIPT=%ROOT_DIR%tools\install-startup-launcher.bat"

for %%S in ("%REBUILD_SCRIPT%" "%DEPENDENCY_SCRIPT%" "%STARTUP_SCRIPT%") do (
    if not exist "%%~fS" (
        echo Required script was not found:
        echo   %%~fS
        exit /b 1
    )
)

echo MultiTool run-to-start setup
echo Root: %ROOT_DIR%
echo.

call "%REBUILD_SCRIPT%"
if errorlevel 1 (
    echo.
    echo Rebuild failed. Stopping run-to-start setup.
    exit /b 1
)

echo.
call "%DEPENDENCY_SCRIPT%"
if errorlevel 1 (
    echo.
    echo Runtime dependency install failed. Stopping run-to-start setup.
    exit /b 1
)

echo.
call "%STARTUP_SCRIPT%"
if errorlevel 1 (
    echo.
    echo Startup launcher install failed. Stopping run-to-start setup.
    exit /b 1
)

echo.
echo MultiTool run-to-start setup completed successfully.

if not defined NO_LAUNCH (
    echo.
    echo Launching MultiTool with administrator access...
    start "" "%ROOT_DIR%MultiTool.exe"
)

exit /b 0
