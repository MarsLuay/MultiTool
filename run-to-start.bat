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
        "try { $argList = @('/c', '""' + $env:SELF_PATH + '"" --elevated ' + $env:RELAUNCH_ARGS); $process = Start-Process -FilePath $env:ComSpec -ArgumentList $argList -Verb RunAs -PassThru -Wait; exit $process.ExitCode } catch { exit 1223 }"
    exit /b %errorlevel%
)

set "ROOT_DIR=%~dp0"
set "REBUILD_SCRIPT=%ROOT_DIR%tools\rebuild-app.bat"
set "DEPENDENCY_SCRIPT=%ROOT_DIR%tools\install-runtime-dependencies.bat"

for %%S in ("%REBUILD_SCRIPT%" "%DEPENDENCY_SCRIPT%") do (
    if not exist "%%~fS" (
        echo Required script was not found:
        echo   %%~fS
        exit /b 1
    )
)

echo MultiTool run-to-start setup
echo Root: %ROOT_DIR%
echo.

pushd "%ROOT_DIR%" >nul
call :sync_latest_main
set "SYNC_EXIT=%errorlevel%"
popd >nul

if not "%SYNC_EXIT%"=="0" (
    echo.
    echo Automatic main update check reported a problem. Continuing with the current files.
)

call "%DEPENDENCY_SCRIPT%"
if errorlevel 1 (
    echo.
    echo Runtime dependency install failed. Stopping run-to-start setup.
    exit /b 1
)

echo.
call "%REBUILD_SCRIPT%"
if errorlevel 1 (
    echo.
    echo Rebuild failed. Stopping run-to-start setup.
    exit /b 1
)

echo.
echo MultiTool run-to-start setup completed successfully.
echo Manage Windows startup from inside MultiTool Settings using Run at startup.

if not defined NO_LAUNCH (
    echo.
    echo Launching MultiTool with administrator access...
    start "" "%ROOT_DIR%MultiTool.exe"
)

exit /b 0

:sync_latest_main
if not exist ".git" (
    echo Git metadata was not found. Skipping automatic main update.
    exit /b 0
)

git rev-parse --is-inside-work-tree >nul 2>&1
if errorlevel 1 (
    echo This folder is not a Git work tree. Skipping automatic main update.
    exit /b 0
)

set "CURRENT_BRANCH="
for /f "usebackq delims=" %%B in (`git rev-parse --abbrev-ref HEAD 2^>nul`) do (
    set "CURRENT_BRANCH=%%B"
)

if not defined CURRENT_BRANCH (
    echo Could not determine the current Git branch. Skipping automatic main update.
    exit /b 1
)

if /I not "%CURRENT_BRANCH%"=="main" (
    echo Current Git branch is "%CURRENT_BRANCH%". Skipping automatic update because run-to-start only auto-syncs main.
    exit /b 0
)

git status --porcelain --untracked-files=normal | findstr /r "." >nul
if not errorlevel 1 (
    echo Local Git changes were found. Skipping automatic main update to protect them.
    exit /b 0
)

git remote get-url origin >nul 2>&1
if errorlevel 1 (
    echo Git remote "origin" was not found. Skipping automatic main update.
    exit /b 0
)

echo Checking origin/main for newer commits...
git fetch origin main --quiet
if errorlevel 1 (
    echo Could not fetch origin/main. Skipping automatic main update.
    exit /b 1
)

set "LOCAL_HEAD="
set "REMOTE_HEAD="
for /f "usebackq delims=" %%L in (`git rev-parse HEAD 2^>nul`) do (
    set "LOCAL_HEAD=%%L"
)
for /f "usebackq delims=" %%R in (`git rev-parse origin/main 2^>nul`) do (
    set "REMOTE_HEAD=%%R"
)

if not defined LOCAL_HEAD (
    echo Could not read the current commit. Skipping automatic main update.
    exit /b 1
)

if not defined REMOTE_HEAD (
    echo Could not read origin/main. Skipping automatic main update.
    exit /b 1
)

if /I "%LOCAL_HEAD%"=="%REMOTE_HEAD%" (
    echo MultiTool is already on the latest origin/main commit.
    exit /b 0
)

echo Updating MultiTool from origin/main...
git pull --ff-only origin main
if errorlevel 1 (
    echo Fast-forward update from origin/main failed.
    exit /b 1
)

echo MultiTool was updated to the latest origin/main commit.
exit /b 0
