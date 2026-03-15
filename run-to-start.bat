@echo off
setlocal EnableExtensions

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
exit /b 0
