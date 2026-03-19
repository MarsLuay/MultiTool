# Tools Folder

This folder contains the remaining repo-level helper scripts for rebuilding, dependency setup, and removing a local MultiTool checkout.

Most people should start with the root-level `run-to-start.bat`. That wrapper still handles the common "set up what is missing, rebuild, then launch" flow for local source usage. The scripts in this folder are the lower-level steps behind that flow.

## What Each Script Is For

### `install-runtime-dependencies.bat`

Checks for and installs the shared machine dependencies that still matter to local builds and a few runtime features:

- `winget`
- `ffmpeg`
- .NET 8 SDK

Use this when:

- you are setting up a new machine for source builds
- video recording is failing because `ffmpeg` is missing
- `run-to-start.bat` told you a dependency needs attention

Important notes:

- `MultiTool.exe` itself is published self-contained, so this script is not required just to open the app
- app-specific installer prerequisites such as Git, Python, 7-Zip, or VC++ are still handled inside MultiTool when those installs actually run
- Windows startup is now managed directly by the app setting instead of external launcher scripts

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

### `uninstall-multitool.bat`

Interactive removal helper for deleting the whole local MultiTool checkout/build folder.

What it does:

- asks for confirmation before continuing
- closes running MultiTool processes
- disables `Run at startup`
- removes leftover legacy Startup-folder shortcuts if they still exist
- schedules deletion of the repository folder after the current terminal gets out of the way

Use this when:

- you want to remove this local copy completely
- you are cleaning up an old checkout and do not need its built executable or scripts anymore

## Recommended Ways To Use This Folder

### First-time source setup

1. Run the root-level `run-to-start.bat`.
2. Let it call the dependency and rebuild helpers for you.
3. Open MultiTool.
4. If you want startup behavior, turn on `Settings > Run at startup` inside the app.

### Rebuild without reinstalling dependencies

1. Run `tools\rebuild-app.bat`.
2. Launch `MultiTool.exe` from the repository root.

### Manage startup

1. Open MultiTool.
2. Use `Settings > Run at startup`.

### Remove the local checkout

1. Run `tools\uninstall-multitool.bat`.
2. Read the prompts carefully before confirming, because it removes the whole local folder rather than just one build output.
