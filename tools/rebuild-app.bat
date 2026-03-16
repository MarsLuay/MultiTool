@echo off
setlocal EnableExtensions

for %%I in ("%~dp0..") do set "ROOT_DIR=%%~fI"
set "SOURCE_DIR=%ROOT_DIR%"
set "PROJECT_FILE=%ROOT_DIR%\src\AutoClicker.App\AutoClicker.App.csproj"
set "RUNTIME_DIR=%ROOT_DIR%"
set "RUNTIME_RESOURCES_DIR=%RUNTIME_DIR%\Resources"
set "RUNTIME_MACROS_DIR=%RUNTIME_DIR%\Macros"
set "LEGACY_APP_DIR=%ROOT_DIR%\app"
set "LEGACY_APP_RESOURCES_DIR=%LEGACY_APP_DIR%\Resources"
set "LEGACY_APP_MACROS_DIR=%LEGACY_APP_DIR%\Macros"
set "LEGACY_APP_EXE=%LEGACY_APP_DIR%\MultiTool.exe"
set "LEGACY_APP_LEGACY_EXE=%LEGACY_APP_DIR%\AutoClicker.exe"
set "ROOT_STAGE_DIR=%ROOT_DIR%\.publish-root"
set "ROOT_PUBLISH_EXE=%ROOT_STAGE_DIR%\MultiTool.exe"
set "RUNTIME_EXE=%RUNTIME_DIR%\MultiTool.exe"
set "RUNTIME_LEGACY_EXE=%RUNTIME_DIR%\AutoClicker.exe"
set "RUNTIME_FFMPEG_EXE=%RUNTIME_DIR%\ffmpeg.exe"
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
echo App EXE output: %RUNTIME_DIR%
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
    echo App publish failed with exit code %EXIT_CODE%.
    exit /b %EXIT_CODE%
)

if exist "%RUNTIME_EXE%" (
    del /q "%RUNTIME_EXE%"
)

if exist "%RUNTIME_LEGACY_EXE%" (
    del /q "%RUNTIME_LEGACY_EXE%"
)

if exist "%LEGACY_APP_EXE%" (
    del /q "%LEGACY_APP_EXE%"
)

if exist "%LEGACY_APP_LEGACY_EXE%" (
    del /q "%LEGACY_APP_LEGACY_EXE%"
)

if exist "%ROOT_DIR%\AutoClicker.App.exe" (
    del /q "%ROOT_DIR%\AutoClicker.App.exe"
)

if not exist "%ROOT_PUBLISH_EXE%" (
    echo.
    echo Expected published app EXE was not created:
    echo   %ROOT_PUBLISH_EXE%
    exit /b 1
)

move /y "%ROOT_PUBLISH_EXE%" "%RUNTIME_EXE%" >nul
if errorlevel 1 (
    echo.
    echo Moving the published EXE into MultiTool.exe failed.
    exit /b 1
)

if exist "%ROOT_STAGE_DIR%\Resources" (
    if exist "%RUNTIME_RESOURCES_DIR%" (
        rmdir /s /q "%RUNTIME_RESOURCES_DIR%"
    )

    xcopy "%ROOT_STAGE_DIR%\Resources" "%RUNTIME_RESOURCES_DIR%\" /E /I /Y >nul
    if errorlevel 1 (
        echo.
        echo Copying Resources into Resources failed.
        exit /b 1
    )
)

if exist "%LEGACY_APP_RESOURCES_DIR%" (
    rmdir /s /q "%LEGACY_APP_RESOURCES_DIR%"
)

if exist "%LEGACY_APP_MACROS_DIR%" (
    if not exist "%RUNTIME_MACROS_DIR%" (
        move "%LEGACY_APP_MACROS_DIR%" "%RUNTIME_MACROS_DIR%" >nul
    ) else (
        xcopy "%LEGACY_APP_MACROS_DIR%" "%RUNTIME_MACROS_DIR%\" /E /I /Y >nul
        if not errorlevel 1 (
            rmdir /s /q "%LEGACY_APP_MACROS_DIR%"
        )
    )
)

if exist "%ROOT_STAGE_DIR%" (
    rmdir /s /q "%ROOT_STAGE_DIR%"
)

echo.
echo Rebuild complete.
echo App EXE: %RUNTIME_EXE%

if exist "%RUNTIME_FFMPEG_EXE%" (
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
