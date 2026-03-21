# Source Overview

This folder contains the main MultiTool application code and the fastest high-level map of what the app actually does.

## Project Layout

- `MultiTool.App`: the WPF shell, tabs, viewmodels, dialogs, startup wiring, tray behavior, and user-facing flows.
- `MultiTool.Core`: shared models, enums, validators, and service contracts that the app and Windows layer build on.
- `MultiTool.Infrastructure.Windows`: Windows-specific implementations for clicking, hotkeys, screenshots, video capture, installer work, telemetry helpers, startup integration, tray metrics, and the Tools tab utilities.

If you are new to the repo, start in `MultiTool.App` to understand the product surface, then follow contracts into `MultiTool.Core`, and only then drop into `MultiTool.Infrastructure.Windows` for the OS-specific behavior.

## What MultiTool Offers

The easiest way to understand the product is by tab, because the source is organized around those user flows.

### Clicker

The Clicker tab is the main auto-clicking toolset. It currently offers:

- configurable interval timing down to milliseconds
- optional random timing variation for less uniform click spacing
- left, right, middle, and custom input modes
- single click, double click, and hold behavior
- repeat forever or repeat a fixed number of times
- current cursor position or fixed screen coordinates
- a configurable global clicker hotkey
- force stop by pressing `Shift + clicker hotkey`
- a short interaction pause so clicking inside MultiTool does not fight the live clicker

### Screenshot

The Screenshot tab uses one configurable hotkey for the full capture flow:

- press once for a full-screen screenshot
- press twice quickly for an area screenshot
- press three times quickly to start area video recording
- press the same hotkey once while recording to stop and save the video
- latest screenshot and latest video status/preview in the tab
- a monitor-corner recording indicator that stays out of the captured video when the platform supports exclusion from capture

### Macro

The Macro tab is the keyboard and mouse automation surface. It includes:

- macro recording
- optional mouse movement capture
- playback once, a fixed number of times, or infinitely
- hotkey-based stop behavior for infinite playback
- save, load, refresh, and edit flows for recorded macros
- per-macro hotkey assignment for saved macros

### Installer

The Installer tab is the software-management side of the app. It includes:

- a catalog of common packages to browse
- install, update, reinstall, and interactive-run actions where supported
- detection of installed state and update state
- custom handling for packages that do not fit a basic `winget` flow
- cleanup picks for bundled or removable apps
- MultiTool update summary and installer operation history

### Tools

The Tools tab is split into several Windows-focused sections. These are the user-facing tools exposed there today.

#### Input and Display

- `Shortcut Key Explorer`: scans shortcut and hotkey sources, shows cached results when reopened, supports rescanning, and can disable supported shortcut-file hotkeys.
- `Display Refresh`: checks display refresh information and applies supported refresh-rate changes.
- `Mouse Speed (DPI Helper)`: reads and updates the Windows mouse sensitivity level.

#### Hardware and Drivers

- `Hardware Check`: collects a compact hardware and software inventory snapshot.
- `Driver Updates`: scans for relevant driver updates and can reuse the already-cached hardware inventory from the current session.

#### Windows Tweaks

- `Dark Mode Helper`: applies the current Windows dark-mode preference through the app.
- `Pin Window`: toggles always-on-top behavior and exposes pinning support.
- `Replace Windows Search`: runs the app's Windows Search replacement workflow.
- `Re-index Windows Search`: requests a Windows Search reindex and reports status.
- `Stop Telemetry`: applies telemetry-reduction steps and compares live IPv4 remote-source counts before and after.
- `Remove OneDrive`: checks status and removes OneDrive.
- `Remove Edge`: checks status and removes Edge where supported.
- `Swap Fn and Ctrl (Lenovo)`: toggles the Lenovo Fn/Ctrl swap on supported systems.
- `Windows 11 EEA Media Prep`: prepares the Windows 11 EEA media-install path.

#### Utilities

- `IPv4 Socket Snapshot`: captures a live IPv4 TCP/UDP snapshot similar to a Windows-side `ss -4`.
- `Useful Sites`: reveals curated external links and opens them in one click.
- `Remove Empty Directories`: scans a folder tree for empty directories and deletes only the selected results.

### Settings

The Settings tab is the app-wide preference and troubleshooting area. It currently includes:

- dark mode
- Ctrl+wheel UI scaling
- always-on-top
- silly/cat-language mode
- `Run at startup`
- auto-hide on startup
- reset-all-settings flow
- copyable activity log for bug reports and troubleshooting

## App-Wide Behavior Outside The Tabs

Some visible MultiTool behavior is not tied to one tab:

- tray icon support for minimizing/keeping the app out of the taskbar until needed
- live tray hover metrics for CPU, temperature, memory, and disk usage
- window pinning state in the title bar
- startup-launch behavior used by the in-app `Run at startup` setting

## Where To Start In The Code

- Start in `MultiTool.App` when you want to trace a button, hotkey flow, or tab-level user experience.
- Start in `MultiTool.Core` when you want the data model or service contract for a feature.
- Start in `MultiTool.Infrastructure.Windows` when you want the real Windows implementation behind that contract.
