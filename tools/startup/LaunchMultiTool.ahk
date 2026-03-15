#Requires AutoHotkey v2.0
#SingleInstance Force

SetWorkingDir A_ScriptDir "\..\..\.."

appPath := A_WorkingDir "\MultiTool.exe"

if !FileExist(appPath)
{
    MsgBox "MultiTool.exe was not found at:`n" appPath, "MultiTool Startup", "Iconx"
    ExitApp
}

Run '"' appPath '"', A_WorkingDir
