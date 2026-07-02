# Terra Launcher ![AppIcon](https://i.imgur.com/x4eChND.png)

[![Latest Release](https://img.shields.io/github/release/trigger-death/TerraLauncher.svg?style=flat&label=version)](https://github.com/trigger-death/TerraLauncher/releases/latest)
[![Latest Release Date](https://img.shields.io/github/release-date-pre/trigger-death/TerraLauncher.svg?style=flat&label=released)](https://github.com/trigger-death/TerraLauncher/releases/latest)
[![Total Downloads](https://img.shields.io/github/downloads/trigger-death/TerraLauncher/total.svg?style=flat)](https://github.com/trigger-death/TerraLauncher/releases)
[![Creation Date](https://img.shields.io/badge/created-september%202017-A642FF.svg?style=flat)](https://github.com/trigger-death/TerraLauncher/commit/cdcf8869a032fd464e045437c6d340e2af51f81c)
[![Terraria Forums](https://img.shields.io/badge/terraria-forums-28A828.svg?style=flat)](https://forums.terraria.org/index.php?threads/62315/)
[![Discord](https://img.shields.io/discord/436949335947870238.svg?style=flat&logo=discord&label=chat&colorB=7389DC&link=https://discord.gg/vB7jUbY)](https://discord.gg/vB7jUbY)

A Terraria-styled hub for everything Terraria-related stored on your computer. Keep track of different game versions, servers, and tools. You can even customize game save directories. Basically it's just a glorified file browser.

![Window Preview](https://i.imgur.com/pdEhK5S.png)

### [Wiki](https://github.com/trigger-death/TerraLauncher/wiki) | [Credits](https://github.com/trigger-death/TerraLauncher/wiki/Credits) | [Image Album](https://imgur.com/a/Qh7aX)

### [![Get Terra Launcher](https://i.imgur.com/8nZihFe.png)](https://github.com/trigger-death/TerraLauncher/releases/latest)

## About

* **Created By:** Robert Jordan
* **Version:** 1.0.0
* **Language:** C#, [Avalonia UI](https://avaloniaui.net/) (cross-platform)
* **Framework:** .NET 10

## Requirements for Running

| Platform | Requirement |
|----------|-------------|
| **Windows** | Windows 10 or later (x64 or arm64) |
| **macOS** | macOS 11 (Big Sur) or later (Apple Silicon) |
| **Linux** | Any modern x64 or arm64 distribution |

Releases are self-contained — no separate .NET runtime installation required.

## Features

* Keep a collection of links to different Terraria executables, servers, and tools
* Auto-detects Steam-installed Terraria on first launch
* Supports Terraria, tModLoader, tAPI, tConfig, and StandAlone instances
* Change Terraria save directory for individual game entries
* Sort entries into folders for better organization
* Open save folder or executable folder directly from the launcher
* Add and remove instances with confirmation dialogs
* Built with Terraria style and sounds to feel more like the game
* Cross-platform: runs on Windows, macOS, and Linux

## Building from Source

### Requirements

* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Run (development)

```sh
dotnet run --project TerraLauncher/TerraLauncher.csproj
```

### Publish

Each publish command produces a self-contained, ready-to-distribute build.

**macOS (Apple Silicon)**
```sh
dotnet publish TerraLauncher/TerraLauncher.csproj -c Release -r osx-arm64 --self-contained -o ./publish/osx-arm64
open ./publish/TerraLauncher.app
```

**Linux (x64)**
```sh
dotnet publish TerraLauncher/TerraLauncher.csproj -c Release -r linux-x64 --self-contained -o ./publish/linux-x64
# Produces: ./publish/linux-x64/ with binary, TerraLauncher.desktop, TerraLauncher.png, install-linux.sh
```

**Linux (arm64)**
```sh
dotnet publish TerraLauncher/TerraLauncher.csproj -c Release -r linux-arm64 --self-contained -o ./publish/linux-arm64
```

**Windows (x64)**
```sh
dotnet publish TerraLauncher/TerraLauncher.csproj -c Release -r win-x64 --self-contained -o ./publish/win-x64
```

**Windows (arm64)**
```sh
dotnet publish TerraLauncher/TerraLauncher.csproj -c Release -r win-arm64 --self-contained -o ./publish/win-arm64
```

## Building on Windows (Git Bash)

Install the [.NET 10 SDK for Windows](https://dotnet.microsoft.com/download/dotnet/10.0), then from Git Bash:

```sh
# Build
dotnet build TerraLauncher/TerraLauncher.csproj

# Run (development)
dotnet run --project TerraLauncher/TerraLauncher.csproj

# Publish a self-contained Windows build
dotnet publish TerraLauncher/TerraLauncher.csproj -c Release -r win-x64 --self-contained -o ./publish/win-x64

# Launch the published build
./publish/win-x64/TerraLauncher.exe
```

The published `TerraLauncher.exe` carries the app icon (from `App.ico`) for Explorer
and the taskbar.

### Linux installation

After publishing for Linux, run the included install script to register the app in your desktop environment:

```sh
cd ./publish/linux-x64
./install-linux.sh
```

This installs TerraLauncher to `~/.local/share/TerraLauncher/`, creates a launcher symlink at `~/.local/bin/TerraLauncher`, and registers the `.desktop` entry so it appears in your applications menu.
