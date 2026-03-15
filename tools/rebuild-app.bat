@echo off
setlocal EnableExtensions

for %%I in ("%~dp0..") do set "ROOT_DIR=%%~fI"
set "SOURCE_DIR=%ROOT_DIR%"
set "PROJECT_FILE=%ROOT_DIR%\src\AutoClicker.App\AutoClicker.App.csproj"
set "ROOT_OUTPUT_DIR=%ROOT_DIR%"
set "ROOT_RESOURCES_DIR=%ROOT_DIR%\Resources"
set "ROOT_STAGE_DIR=%ROOT_DIR%\.publish-root"
set "ROOT_PUBLISH_EXE=%ROOT_STAGE_DIR%\MultiTool.exe"
set "ROOT_FINAL_EXE=%ROOT_OUTPUT_DIR%\MultiTool.exe"
set "ROOT_LEGACY_EXE=%ROOT_OUTPUT_DIR%\AutoClicker.exe"
set "ROOT_FFMPEG_EXE=%ROOT_OUTPUT_DIR%\ffmpeg.exe"
set "DEPENDENCY_INSTALLER=%ROOT_DIR%\tools\install-runtime-dependencies.bat"
set "DOTNET_EXE=%USERPROFILE%\.dotnet\dotnet.exe"
set "PROGRAM_FILES_DOTNET=%ProgramFiles%\dotnet\dotnet.exe"

if not exist "%PROJECT_FILE%" (
    echo Could not find the app project:
    echo   %PROJECT_FILE%
    exit /b 1
)

if not exist "%DOTNET_EXE%" (
    if exist "%PROGRAM_FILES_DOTNET%" (
        set "DOTNET_EXE=%PROGRAM_FILES_DOTNET%"
        goto dotnet_ready
    )

    where dotnet >nul 2>&1
    if errorlevel 1 (
        echo Could not find dotnet. Install .NET 8 SDK or add dotnet to PATH.
        exit /b 1
    )

    set "DOTNET_EXE=dotnet"
)

:dotnet_ready

echo Rebuilding MultiTool...
echo Source: %PROJECT_FILE%
echo Root EXE output: %ROOT_OUTPUT_DIR%
echo.

taskkill /IM MultiTool.exe /F /T >nul 2>&1
if not errorlevel 1 (
    echo Closed running MultiTool.exe.
    timeout /t 1 /nobreak >nul
)

taskkill /IM AutoClicker.exe /F /T >nul 2>&1
if not errorlevel 1 (
    echo Closed running AutoClicker.exe.
    timeout /t 1 /nobreak >nul
)

pushd "%SOURCE_DIR%" >nul
if exist "%ROOT_STAGE_DIR%" (
    rmdir /s /q "%ROOT_STAGE_DIR%"
)

"%DOTNET_EXE%" publish "%PROJECT_FILE%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o "%ROOT_STAGE_DIR%"
set "EXIT_CODE=%ERRORLEVEL%"
popd >nul

if not "%EXIT_CODE%"=="0" (
    echo.
    echo Root EXE publish failed with exit code %EXIT_CODE%.
    exit /b %EXIT_CODE%
)

if exist "%ROOT_FINAL_EXE%" (
    del /q "%ROOT_FINAL_EXE%"
)

if exist "%ROOT_LEGACY_EXE%" (
    del /q "%ROOT_LEGACY_EXE%"
)

if exist "%ROOT_OUTPUT_DIR%AutoClicker.App.exe" (
    del /q "%ROOT_OUTPUT_DIR%AutoClicker.App.exe"
)

if exist "%ROOT_DIR%app" (
    rmdir /s /q "%ROOT_DIR%app"
)

if not exist "%ROOT_PUBLISH_EXE%" (
    echo.
    echo Expected published root EXE was not created:
    echo   %ROOT_PUBLISH_EXE%
    exit /b 1
)

move /y "%ROOT_PUBLISH_EXE%" "%ROOT_FINAL_EXE%" >nul
if errorlevel 1 (
    echo.
    echo Moving the root EXE to MultiTool.exe failed.
    exit /b 1
)

if exist "%ROOT_STAGE_DIR%\Resources" (
    if exist "%ROOT_RESOURCES_DIR%" (
        rmdir /s /q "%ROOT_RESOURCES_DIR%"
    )

    xcopy "%ROOT_STAGE_DIR%\Resources" "%ROOT_RESOURCES_DIR%\" /E /I /Y >nul
    if errorlevel 1 (
        echo.
        echo Copying Resources to the root publish failed.
        exit /b 1
    )
)

if exist "%ROOT_STAGE_DIR%" (
    rmdir /s /q "%ROOT_STAGE_DIR%"
)

echo.
echo Rebuild complete.
echo Root EXE: %ROOT_FINAL_EXE%

if exist "%ROOT_FFMPEG_EXE%" (
    echo Video recording dependency check: ffmpeg.exe found next to MultiTool.exe.
    exit /b 0
)

where ffmpeg >nul 2>&1
if not errorlevel 1 (
    echo Video recording dependency check: ffmpeg found on PATH.
    exit /b 0
)

echo.
echo WARNING: ffmpeg was not found.
echo Local video recording will not work until ffmpeg is installed.
if exist "%DEPENDENCY_INSTALLER%" (
    echo Run this to install missing runtime dependencies:
    echo   %DEPENDENCY_INSTALLER%
) else (
    echo Install ffmpeg manually or add ffmpeg.exe next to MultiTool.exe.
)
exit /b 0
