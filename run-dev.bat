@echo off
setlocal EnableExtensions

set "ROOT_DIR=%~dp0"
set "REBUILD_SCRIPT=%ROOT_DIR%tools\rebuild-app.bat"
set "APP_EXE=%ROOT_DIR%MultiTool.exe"
set "NO_LAUNCH="

if /I "%~1"=="--no-launch" (
    set "NO_LAUNCH=1"
    shift
)

if not exist "%REBUILD_SCRIPT%" (
    echo Could not find rebuild script:
    echo   %REBUILD_SCRIPT%
    exit /b 1
)

echo MultiTool dev run
echo Root: %ROOT_DIR%
echo.

call "%REBUILD_SCRIPT%"
if errorlevel 1 (
    echo.
    echo Rebuild failed. Stopping dev launch.
    exit /b 1
)

if not exist "%APP_EXE%" (
    echo.
    echo Could not find built app:
    echo   %APP_EXE%
    exit /b 1
)

echo.
echo Launching MultiTool with memory logging enabled...
echo Logs: %ROOT_DIR%Logs

if defined NO_LAUNCH (
    echo Skipping launch because --no-launch was provided.
    exit /b 0
)

start "" "%APP_EXE%" --log-memory --trace-tabs %*

exit /b 0
