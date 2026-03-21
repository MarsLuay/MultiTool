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

pushd "%ROOT_DIR%" >nul
call :sync_latest_main
set "SYNC_EXIT=%errorlevel%"
popd >nul

if not "%SYNC_EXIT%"=="0" (
    echo.
    echo Automatic main update check reported a problem. Continuing with the current files.
)

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
    echo Current Git branch is "%CURRENT_BRANCH%". Skipping automatic update because run-dev only auto-syncs main.
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
