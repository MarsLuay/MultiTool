# Tools Folder

This folder contains the repo-level helper scripts for building, repairing, launching, starting with Windows, and removing MultiTool.

Most people should start with the root-level `run-to-start.bat`. That wrapper handles the common "set up what is missing, rebuild, then launch" flow for you. The scripts in this folder are for the lower-level steps when you want to run one piece directly.

## What Each Script Is For

### `install-runtime-dependencies.bat`

Checks for and installs the shared machine dependencies that matter to local builds and a few runtime features:

- `winget`
- `ffmpeg`
- AutoHotkey v2
- .NET 8 SDK

Use this when:

- you are setting up a new machine for source builds
- video recording is failing because `ffmpeg` is missing
- `run-to-start.bat` told you a dependency needs attention

Important notes:

- `MultiTool.exe` itself is published self-contained, so this script is not required just to open the app
- app-specific installer prerequisites such as Git, Python, 7-Zip, or VC++ are still handled inside MultiTool when those installs actually run
- AutoHotkey is now mostly kept for legacy startup-launcher compatibility, not as the main startup path

### `rebuild-app.bat`

Builds and republishes the desktop app from source, then refreshes the root-level `MultiTool.exe`.

It currently does all of this for you:

- closes a running `MultiTool.exe` if one is already open
- publishes `src\MultiTool.App\MultiTool.App.csproj`
- copies the fresh build output to the repository root
- keeps supporting assets such as `Resources`
- carries forward legacy `Macros` data when needed
- warns if `ffmpeg` is not available for video recording support

Use this when:

- you changed source code and want a fresh local executable
- you want the publish step without running the full setup wrapper
- you are debugging startup or packaging behavior around the root `MultiTool.exe`

### `install-startup-launcher.bat`

Turns on Windows startup for the current user by creating the same per-user `Run` registry entry that the in-app `Settings > Run at startup` toggle uses.

What it also cleans up:

- old Startup-folder shortcut installs from earlier versions
- legacy launcher names such as `Launch MultiTool.lnk`
- older compatibility names such as `Launch AutoClicker.lnk`

Use this when:

- you want to enable startup from the command line
- you need to repair the startup registration without opening the app

Preferred path:

- for everyday use, change this inside MultiTool with `Settings > Run at startup`

### `remove-startup-launcher.bat`

Turns off that same per-user `Run` registration and removes any leftover legacy Startup-folder shortcuts.

Use this when:

- you want to disable startup from the command line
- you are cleaning up an older install that still has shortcut-based startup artifacts

### `uninstall-multitool.bat`

Interactive removal helper for deleting the whole local MultiTool checkout/build folder.

What it does:

- asks for confirmation before continuing
- closes running MultiTool processes
- disables `Run at startup`
- calls the startup-cleanup helper first
- schedules deletion of the repository folder after the current terminal gets out of the way

Use this when:

- you want to remove this local copy completely
- you are cleaning up an old checkout and do not need its built executable or scripts anymore

## `startup` Subfolder

### `startup\LaunchMultiTool.ahk`

Legacy AutoHotkey launcher that starts:

`MultiTool.exe --startup-launch`

This file is still kept for compatibility with older shortcut-based startup paths. Newer installs should not depend on it. Prefer:

1. `Settings > Run at startup` inside MultiTool.
2. `tools\install-startup-launcher.bat` if you need the same action from the command line.

## Recommended Ways To Use This Folder

### First-time source setup

1. Run the root-level `run-to-start.bat`.
2. Let it call the dependency and rebuild helpers for you.
3. Open MultiTool.
4. If you want startup behavior, turn on `Settings > Run at startup` inside the app.

### Rebuild without reinstalling dependencies

1. Run `tools\rebuild-app.bat`.
2. Launch `MultiTool.exe` from the repository root.

### Repair startup registration

1. Open MultiTool and use `Settings > Run at startup` if the app is available.
2. Use `tools\install-startup-launcher.bat` or `tools\remove-startup-launcher.bat` only when you specifically want the command-line version of that same toggle.

### Remove the local checkout

1. Run `tools\uninstall-multitool.bat`.
2. Read the prompts carefully before confirming, because it removes the whole local folder rather than just one build output.
