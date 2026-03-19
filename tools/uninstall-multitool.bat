@echo off
setlocal EnableExtensions

for %%I in ("%~dp0..") do set "ROOT_DIR=%%~fI"
set "CLEANUP_SCRIPT=%TEMP%\multitool-uninstall-%RANDOM%%RANDOM%.cmd"
set "RUN_KEY=HKCU\Software\Microsoft\Windows\CurrentVersion\Run"
set "RUN_VALUE_NAME=MultiTool"
set "STARTUP_DIR=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup"
set "SHORTCUT_PATH=%STARTUP_DIR%\Launch MultiTool.lnk"
set "LEGACY_SHORTCUT_PATH=%STARTUP_DIR%\Launch AutoClicker.lnk"

echo MultiTool uninstall helper
echo.
echo This will permanently delete the entire MultiTool folder:
echo   %ROOT_DIR%
echo.
echo That includes:
echo   - MultiTool.exe
echo   - Logs, Macros, and Resources
echo   - src, tests, and the tools folder
echo   - run-to-start.bat, the helper scripts, and the current Run at startup registration
echo.
echo This cannot be undone.
echo.
set /p "CONFIRM_TEXT=Type DELETE MULTITOOL to continue: "
if /I not "%CONFIRM_TEXT%"=="DELETE MULTITOOL" (
    echo.
    echo Uninstall canceled.
    exit /b 0
)

choice /C YN /N /M "Final confirmation - delete everything in this MultiTool folder? [Y/N] "
if errorlevel 2 (
    echo.
    echo Uninstall canceled.
    exit /b 0
)

echo.
echo Closing MultiTool if it is running...
taskkill /IM MultiTool.exe /F /T >nul 2>&1
taskkill /IM MultiTool.exe /F /T >nul 2>&1

reg delete "%RUN_KEY%" /v "%RUN_VALUE_NAME%" /f >nul 2>&1
if exist "%SHORTCUT_PATH%" del /q "%SHORTCUT_PATH%" >nul 2>&1
if exist "%LEGACY_SHORTCUT_PATH%" del /q "%LEGACY_SHORTCUT_PATH%" >nul 2>&1

> "%CLEANUP_SCRIPT%" echo @echo off
>>"%CLEANUP_SCRIPT%" echo setlocal EnableExtensions
>>"%CLEANUP_SCRIPT%" echo set "TARGET_DIR=%ROOT_DIR%"
>>"%CLEANUP_SCRIPT%" echo :wait_for_delete
>>"%CLEANUP_SCRIPT%" echo timeout /t 2 /nobreak ^>nul
>>"%CLEANUP_SCRIPT%" echo rmdir /s /q "%%TARGET_DIR%%" ^>nul 2^>^&1
>>"%CLEANUP_SCRIPT%" echo if exist "%%TARGET_DIR%%\\" goto wait_for_delete
>>"%CLEANUP_SCRIPT%" echo del /q "%%~f0" ^>nul 2^>^&1

start "" /min cmd /c ""%CLEANUP_SCRIPT%""

echo.
echo Uninstall scheduled.
echo If you launched this from a terminal that is still inside %ROOT_DIR%,
echo close that terminal so Windows can finish deleting the folder.
echo.
echo This window can now close.
exit /b 0
