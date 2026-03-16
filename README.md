# MultiTool

MultiTool is a Windows desktop application for click automation, screenshots, and macros.

## Status

This repository contains the current .NET 8 MultiTool codebase with a split between app, core logic, and Windows-specific infrastructure.

## Current solution layout

- repository root
  - runtime layout for the published EXE, macros, icons, and logs
- `src/AutoClicker.App`
  - WPF shell, view models, and dialog services
- `src/AutoClicker.Core`
  - domain models, validation, and click-loop orchestration
- `src/AutoClicker.Infrastructure.Windows`
  - Win32 hotkeys, `SendInput` mouse execution, JSON settings persistence, and tray integration
- `tests/AutoClicker.Core.Tests`
  - core behavior tests
- `tests/AutoClicker.Infrastructure.Windows.Tests`
  - migration and Windows infrastructure tests

## Features

- custom click interval down to `1 ms`
- left, right, and middle mouse buttons
- single-click and double-click modes
- infinite repeat or fixed repeat counts
- current cursor position or captured screen coordinates
- start / stop / toggle hotkeys
- tray support and persisted settings

## Platform

Windows only.

## Build

The rewrite targets `.NET 8` and expects a .NET 8 SDK to be installed locally.

Open [AutoClicker.sln](./AutoClicker.sln) in Visual Studio 2022 or later, or build from a terminal after installing the SDK:

```powershell
dotnet restore
dotnet build
dotnet test
```

## Helper scripts

- `run-to-start.bat`
  - root convenience script that rebuilds the app into the repo root, installs runtime dependencies, and installs the startup launcher
- `tools\rebuild-app.bat`
- `tools\install-runtime-dependencies.bat`
- `tools\install-startup-launcher.bat`
- `tools\remove-startup-launcher.bat`
- `tools\uninstall-multitool.bat`
