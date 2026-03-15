@echo off
setlocal EnableExtensions

for %%I in ("%~dp0.") do set "ROOT_DIR=%%~fI"
set "SHORTCUT_REMOVER=%ROOT_DIR%\remove-startup-launcher.bat"
set "CLEANUP_SCRIPT=%TEMP%\multitool-uninstall-%RANDOM%%RANDOM%.cmd"

echo MultiTool uninstall helper
echo.
echo This will permanently delete the entire MultiTool folder:
echo   %ROOT_DIR%
echo.
echo That includes:
echo   - MultiTool.exe
echo   - Logs, Macros, and Resources
echo   - the source folder
echo   - every root batch file, including this one
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
taskkill /IM AutoClicker.exe /F /T >nul 2>&1

if exist "%SHORTCUT_REMOVER%" (
    call "%SHORTCUT_REMOVER%" >nul 2>&1
)

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
