# VirtuaSwitcher

![VirtuaSwitcher](Resources/icon.png)

A lightweight Windows system tray application for switching between display configuration presets — designed for setups where you regularly switch between multiple monitor arrangements (e.g., a desktop workstation and a TV).

[![Release](https://github.com/masonkuck/VirtuaSwitcher/actions/workflows/release.yml/badge.svg)](https://github.com/masonkuck/VirtuaSwitcher/actions/workflows/release.yml)
[![Pre-Release](https://github.com/masonkuck/VirtuaSwitcher/actions/workflows/prerelease.yml/badge.svg)](https://github.com/masonkuck/VirtuaSwitcher/actions/workflows/prerelease.yml)
## Features

- **Display presets**: capture and name any display configuration, including active monitors, resolution, refresh rate, and layout
- **Global hotkeys**: switch presets from anywhere with a configurable keyboard shortcut
- **System tray**: lives in the notification area; double-click or right-click to access presets and settings
- **Launch on startup** : optionally start with Windows
- **Up to 10 displays** supported

## Requirements

- Windows 10 or 11 (x64 or x86)
- No additional runtime required (self-contained build)

## Installation

1. Download `VirtuaSwitcher.exe` from the [Releases](../../releases) page
2. Place it in a permanent location (e.g. `%LOCALAPPDATA%\VirtuaSwitcher\`)
3. Run it — the icon will appear in the system tray

> **Note on Windows SmartScreen:** The executable is currently unsigned. Windows may show a "Windows protected your PC" warning on first run — click **More info → Run anyway** to proceed. Code signing requires an established download reputation, which this project is still building. If sufficient users have downloaded and run the app, SmartScreen will stop warning automatically. If I am able to, I will set up signing eventually. This requires a reputation that this project does not have currently.

> **Note:** Place the exe in its final location before enabling "Launch on startup." The startup registry entry is written to wherever the exe currently lives.

## Usage

### Creating a preset

1. Open Settings (double-click the tray icon or right-click → **Settings...**)
2. Click **+ New Preset** and give it a name
3. Arrange your displays the way you want them (resolution, which monitors are active, etc.) using Windows display settings
4. Click **Capture Current Display Config**
5. Optionally assign a hotkey by clicking the hotkey field and pressing your desired key combination

Changes save automatically.

### Switching presets

- **Hotkey**: press the assigned key combination from anywhere
- **Tray menu**: right-click the tray icon and click a preset name
- **Settings window**: select a preset and click **Apply Now**

## Releases

Releases are built automatically via GitHub Actions when a version tag is pushed. Both `win-x64` and `win-x86` executables are attached to each release.

To cut a release:

```powershell
git tag v1.2.3
git push origin v1.2.3
```

## Building from Source

**.NET 10 SDK** is required.

```powershell
# Clone and build
git clone https://github.com/masonkuck/VirtuaSwitcher.git
cd VirtuaSwitcher
dotnet build

# Publish self-contained single-file executables (win-x64 and win-x86)
.\publish.ps1
```

Output is written to `./publish/win-x64/VirtuaSwitcher.exe` and `./publish/win-x86/VirtuaSwitcher.exe`.

## What Gets Captured

Each preset stores a snapshot of the active display topology via the Windows CCD API (`QueryDisplayConfig`):

| Setting | Captured |
|---|---|
| Which monitors are active | ✓ |
| Resolution (per monitor) | ✓ |
| Refresh rate | ✓ |
| Monitor positions / arrangement | ✓ |
| Primary display | ✓ |
| Rotation | ✓ |
| UI scaling (DPI %) | ✗ |
| HDR / Night Light | ✗ |

## Tech Stack

- .NET 10 / WPF
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- [Microsoft.Extensions.Hosting](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host) (dependency injection)
- Windows CCD API via P/Invoke
- [AudioSwitcher.AudioApi.CoreAudio](https://github.com/xenolightning/AudioSwitcher) (audio device switching)
