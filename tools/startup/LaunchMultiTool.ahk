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

if A_IsAdmin
{
    Run '"' appPath '" --startup-launch', rootDir
    ExitApp
}

try
{
    Run '*RunAs "' appPath '" --startup-launch', rootDir
}
catch
{
    MsgBox "MultiTool needs administrator access to read protected hardware telemetry on this PC.`n`nIf you canceled the Windows prompt, launch it again and allow the elevation request.", "MultiTool Startup", "Icon!"
}

ExitApp
