# Terra Launcher ![AppIcon](https://i.imgur.com/x4eChND.png)

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
   dotnet build TerraLauncher.csproj -c Debug
   dotnet run --project TerraLauncher.csproj -c Debug
   ```

NuGet packages (`Extended.Wpf.Toolkit`, `System.Configuration.ConfigurationManager`) restore automatically on
build — no manual setup needed.

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
* Change the save directory per instance (defaults to the game's normal save location, e.g.
  `Documents/My Games/Terraria`, or `.../ModLoader` for tModLoader).
* Remove an instance from the list — for versions installed via the downloader, this also deletes the
  downloaded files; Steam-detected and Custom-linked executables are only removed from the list.
* Search and filter instances by category from the main window.
* Terraria-themed window chrome, dialogs, and sounds throughout.
* Automatically detects existing Steam installs of Terraria and tModLoader on first launch.

## Where things are stored

Everything lives next to `TerraLauncher.exe`:

* `TerraLauncher.xml` — your instance list and settings.
* `Instances/<Category>/` — files for versions installed through the in-app downloader (e.g.
  `Instances/TModLoader/TModLoader-<version>/`).
* `Tools/DepotDownloader/` — the auto-downloaded DepotDownloader binary used for Steam downloads.

Steam-detected installs and Custom-linked executables are referenced in place and are never copied or moved.

## Notes

* 7-Zip archives are intentionally not supported as a download/extraction format — only `.zip` and `.tar.gz`/`.tgz`
  are handled.
* Downloading Terraria requires a Steam account that owns the game; credentials are passed directly to
  DepotDownloader for that single download and are never saved by the launcher.

## License

MIT — see [License](License).
