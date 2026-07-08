# Terra Launcher ![AppIcon](https://i.imgur.com/x4eChND.png)

[![CI](https://github.com/MrPoloGit/TerraLauncher/actions/workflows/ci.yml/badge.svg)](https://github.com/MrPoloGit/TerraLauncher/actions/workflows/ci.yml)
[![Terraria Forums](https://img.shields.io/badge/terraria-forums-28A828.svg?style=flat)](https://forums.terraria.org/index.php?threads/62315/)
[![Discord](https://img.shields.io/discord/436949335947870238.svg?style=flat&logo=discord&label=chat&colorB=7389DC&link=https://discord.gg/vB7jUbY)](https://discord.gg/vB7jUbY)

A Terraria-styled hub for everything Terraria-related stored on your computer. Keep track of different game
versions, mod loaders, and standalone tools, browse and install new versions straight from the launcher, and
customize save directories per instance. Built with Terraria style and sounds to feel more like the game.

![Window Preview](https://i.imgur.com/pdEhK5S.png)

This is a modernized fork of the original [TerraLauncher](https://github.com/trigger-death/TerraLauncher) by
Robert Jordan, ported from .NET Framework 4.5.2 to .NET 10 and extended with an in-app instance downloader.

## About

* **Original Author:** Robert Jordan
* **Language:** C#, WPF
* **Target Framework:** .NET 10 (Windows only)

## Requirements for Running

* [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (Windows) — not required if you use
  a self-contained build
* Windows 10 or later

## Running from Source

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
2. Clone the repository and open `TerraLauncher.sln` in Visual Studio 2022+ (or use the CLI below).
3. Build and run:

   ```
   dotnet build TerraLauncher/TerraLauncher.csproj -c Debug
   dotnet run --project TerraLauncher/TerraLauncher.csproj -c Debug
   ```

NuGet packages (`Extended.Wpf.Toolkit`, `System.Configuration.ConfigurationManager`) restore automatically on
build — no manual setup needed.

## Running Tests

Unit tests cover the pure logic behind the instance downloader (manifest/release parsing, install-path naming,
archive-type detection):

```
dotnet test TerraLauncher.sln
```

CI runs the same build and test suite on every push and pull request via
[GitHub Actions](.github/workflows/ci.yml).

## Publishing

Pushing a `v*` tag (e.g. `v1.2.3`) triggers [a release build](.github/workflows/release.yml) that runs the tests,
then publishes both a portable zip and a Windows installer to GitHub Releases automatically. To do the same thing
locally:

1. Publish a self-contained, single-file build:

   ```
   dotnet publish TerraLauncher/TerraLauncher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
   ```

   This produces `publish/TerraLauncher.exe`, which runs on a machine with no .NET runtime installed at all — you
   can zip up the `publish/` folder as-is and hand it out.

2. (Optional) Build the installer from that publish output with [Inno Setup 6](https://jrsoftware.org/isinfo.php):

   ```
   iscc TerraLauncher.iss
   iscc /DMyAppVersion=1.2.3 TerraLauncher.iss
   ```

   This produces `installer-output/TerraLauncher-<version>-Setup.exe`. The installer defaults to installing under
   `%LocalAppData%\Programs\TerraLauncher` (no admin rights required) rather than Program Files, since TerraLauncher
   keeps its config and all downloaded instances next to the exe and needs that folder to stay writable. Uninstalling
   only removes the files the installer put there — `TerraLauncher.xml`, `Instances/`, and `Tools/` are left alone
   so you don't lose downloaded games.

## Features

* Keep a collection of Terraria, tModLoader, tAPI, tConfig, standalone (e.g. `TerrariaServer.exe`), and custom
  executables, sorted into folders with custom icons and details.
* **Add Instance** browses and downloads new versions directly from the launcher:
  * **Terraria** — installed via [DepotDownloader](https://github.com/SteamRE/DepotDownloader) using your Steam
    credentials. DepotDownloader is fetched automatically on first use. The version list is pulled live from a
    community-maintained Steam manifest database, so it always reflects the versions actually available on Steam
    (the currently-installed latest build is filtered out of the list).
  * **tModLoader** — versions are pulled live (with paging) from the
    [tModLoader GitHub releases](https://github.com/tModLoader/tModLoader/releases) and downloaded directly.
  * **tAPI** / **tConfig** — downloaded from archived releases on the Internet Archive.
  * **Custom** — link any existing executable on disk instead of downloading anything.
  * Versions that require a specific Terraria build (e.g. tAPI/tModLoader builds) will offer to install that
    Terraria version first if it isn't already present.
  * Already-installed versions are greyed out and marked "Installed" in the picker.
* Every instance installed through the downloader gets its own save directory under `Documents/My
  Games/TerraLauncher/` (see below) — install ten versions of tModLoader and each keeps its own worlds,
  players, and mods. The save directory is editable per instance if you'd rather point it elsewhere.
* **tAPI** / **tConfig** instances get a **Launch Mod Builder** button (wrench icon) next to Launch Game
  when the downloader finds a mod-packaging tool bundled with that version.
* Remove an instance from the list — for versions installed via the downloader, this also deletes the
  downloaded files; Steam-detected and Custom-linked executables are only removed from the list.
* Search and filter instances by category from the main window.
* Terraria-themed window chrome, dialogs, and sounds throughout.
* Automatically detects existing Steam installs of Terraria and tModLoader on first launch.

## Where things are stored

Game files and save data are split, the same way Steam and Terraria split them (Steam's
`steamapps/common/Terraria` vs. `Documents/My Games/Terraria`):

* Next to `TerraLauncher.exe`:
  * `TerraLauncher.xml` — your instance list and settings.
  * `Instances/<Category>/` — the installed files for versions installed through the in-app
    downloader (e.g. `Instances/TModLoader/TModLoader-<version>/`).
  * `Tools/DepotDownloader/` — the auto-downloaded DepotDownloader binary used for Steam downloads.
* Under `Documents/My Games/TerraLauncher/Instances/<Category>/<version>/` — every downloaded version
  gets its own `Worlds/` and `Players/` folder, plus a `Mods/` folder for tModLoader and tAPI, so
  installing multiple versions never mixes their saves. tConfig instead gets its `ModPacks/` and
  `ModPacks_temp_runtime/` folders (which is where it actually looks for mods) junctioned in here.

Steam-detected installs and Custom-linked executables are referenced in place and are never copied or moved.

## Notes

* 7-Zip archives are intentionally not supported as a download/extraction format — only `.zip` and `.tar.gz`/`.tgz`
  are handled.
* Downloading Terraria requires a Steam account that owns the game; credentials are passed directly to
  DepotDownloader for that single download and are never saved by the launcher.

Windows
Upon fresh install and first launch of Terraria
Documents/My Games/Terraria
Players
Worlds
Program Files (x86)/Steam/userdata/279170712/105600/remote
ModLoader
players
Worlds
Need a way to install and store seperate world/player files
Get DepotDownloader fully working
Observer the result of downloading TModLoader
should maybe use tModLoader first to do it
Then tConfig
Figure out how to take a

## License

MIT — see [License](License).
