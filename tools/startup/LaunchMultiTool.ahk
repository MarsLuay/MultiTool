#Requires AutoHotkey v2.0
#SingleInstance Force

rootDir := A_ScriptDir "\..\.."
SetWorkingDir rootDir

appPath := rootDir "\MultiTool.exe"

if !FileExist(appPath)
{
    MsgBox "MultiTool.exe was not found at:`n" appPath, "MultiTool Startup", "Iconx"
    ExitApp
}

try
{
    Run '"' appPath '" --startup-launch', rootDir
}
catch
{
    MsgBox "MultiTool could not be started from the Startup shortcut.`n`nTry launching MultiTool.exe directly to confirm the app is still present.", "MultiTool Startup", "Icon!"
}

ExitApp
