using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TerraLauncher.Setups;
using TerraLauncher.Util;
using TerraLauncher.Windows;

namespace TerraLauncher.Instances {
	// HTTP download + archive extraction backend for tModLoader/tAPI/tConfig/StandAlone.
	public static class Downloader {
		private static readonly HttpClient http = new HttpClient();

		static Downloader() {
			http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "TerraLauncher/1.0");
			http.Timeout = TimeSpan.FromMinutes(30);
		}

		// Downloads entry.Url, extracts it into installDir (if it's an archive), and
		// returns the detected executable path (null on failure) plus, for
		// categories that ship one, the path to a bundled mod builder/compiler tool.
		public static async Task<(string ExePath, string ModBuilderPath)> InstallFromUrlAsync(DownloadProgressWindow ui,
			VersionEntry entry, GameCategory category, string installDir, CancellationToken ct) {
			if (string.IsNullOrEmpty(entry.Url)) {
				ui.AppendLog("No download URL for this version.");
				return (null, null);
			}

			Directory.CreateDirectory(installDir);

			bool isPrism = category == GameCategory.StandAlone && entry.Type == "Prism";

			// tConfig, Prism, Prepare to Die, and Avalon all need a real Terraria
			// install alongside them - tConfig itself is a runnable build, but still
			// reads/writes several folders (see LinkTConfigModFolders below) and
			// behaves correctly only when it's actually sitting in a proper
			// Terraria-shaped folder; Prism's patcher.exe patches a Terraria.exe in
			// place; Prepare to Die is a replacement exe that needs the original's
			// Content folder alongside it; Avalon's zips only ship its own changed
			// files and overwrite them onto a real Terraria install rather than
			// including a full Content folder of their own. Seed the instance folder
			// with the Steam-installed Terraria files before downloading/extracting
			// anything else on top.
			if (NeedsTerrariaBaseCopy(category, entry)) {
				string terrariaDir = FindTerrariaBaseCopySource(entry);
				if (!string.IsNullOrEmpty(terrariaDir) && Directory.Exists(terrariaDir)) {
					ui.AppendLog("Copying Terraria files from " + terrariaDir);
					ui.SetProgress(-1);
					try {
						await Task.Run(() => CopyDirectory(terrariaDir, installDir, ct), ct);
					}
					catch (OperationCanceledException) { throw; }
					catch (Exception ex) {
						ui.AppendLog("Failed to copy Terraria files: " + ex.Message);
						return (null, null);
					}
				}
				else if (!string.IsNullOrEmpty(entry.RequiresTerrariaVersion)) {
					ui.AppendLog(entry.Name + " needs Terraria " + entry.RequiresTerrariaVersion + " as a base, but it " +
						"isn't installed - download it first (Add Instance > Terraria), then reinstall " + entry.Name + ".");
				}
				else {
					ui.AppendLog("No Steam install of Terraria was found - " + entry.Name + " may not run without one.");
				}
			}

			// Direct file (e.g. an installer .exe) — no extraction step, so there's
			// nothing else in installDir to detect a mod builder from.
			if (!IsArchiveUrl(entry.Url)) {
				string fileName = Path.GetFileName(new Uri(entry.Url).LocalPath);
				if (string.IsNullOrEmpty(fileName)) fileName = "download.bin";
				string dest = Path.Combine(installDir, fileName);
				try {
					ui.AppendLog("Downloading " + entry.Url);
					await DownloadFileAsync(entry.Url, dest, ui, ct);
					ui.AppendLog("Installed: " + dest);
					return (dest, null);
				}
				catch (OperationCanceledException) {
					TryDelete(dest);
					throw;
				}
				catch (Exception ex) {
					ui.AppendLog("Download failed: " + ex.Message);
					TryDelete(dest);
					return (null, null);
				}
			}

			string archivePath = Path.Combine(installDir, "_download" + ArchiveExtension(entry.Url));

			try {
				ui.AppendLog("Downloading " + entry.Url);
				await DownloadFileAsync(entry.Url, archivePath, ui, ct);

				// Some zips (e.g. archive.org's "tConfig 0.38.zip", or GitHub's
				// "Terraria-v1.0.2.zip") wrap everything in one top-level folder
				// rather than extracting flat, so it'd land in installDir/tConfig
				// 0.38/ instead of installDir/ itself. Figure that out before
				// extracting since the archive is deleted right after.
				string zipRootFolder = GetZipRootFolder(archivePath);

				ui.AppendLog("Extracting...");
				ui.SetProgress(-1);
				await Task.Run(() => Extract(archivePath, installDir), ct);
				File.Delete(archivePath);

				if (zipRootFolder != null) {
					string wrapperPath = Path.Combine(installDir, zipRootFolder);
					if (Directory.Exists(wrapperPath)) {
						ui.AppendLog("Flattening " + zipRootFolder + "...");
						await Task.Run(() => CopyDirectory(wrapperPath, installDir, ct), ct);
						Directory.Delete(wrapperPath, recursive: true);
					}
				}

				if (!string.IsNullOrEmpty(entry.PatchUrl)
					&& !await ApplyPatchAsync(ui, entry.PatchUrl, installDir, ct))
					return (null, null);

				// Prism ships as a bare patcher.exe + Prism.exe, not a pre-patched
				// game - the release's own INSTALL.md says to "drop Terraria.exe on
				// patcher.exe" to produce Prism.Terraria.dll before Prism.exe can run.
				if (isPrism && !await RunPrismPatcherAsync(ui, installDir, ct))
					return (null, null);

				string exe;
				if (isPrism) {
					exe = Path.Combine(installDir, "Prism.exe");
					if (!File.Exists(exe)) {
						ui.AppendLog("Patched, but Prism.exe was not found in " + installDir);
						return (null, null);
					}
				}
				else {
					exe = FindExecutable(installDir, category, entry.Type);
					if (exe == null) {
						ui.AppendLog("Extracted, but no executable found in " + installDir);
						return (null, null);
					}
				}
				ui.AppendLog("Installed: " + exe);

				// tConfig creates its own mod-management folders (ModPacks,
				// ModPacks_temp_runtime) relative to its own exe rather than
				// honoring -savedirectory (it doesn't support that flag at all - see
				// TConfigModFolders below), so they'd otherwise end up inside
				// installDir instead of next to Worlds/Players/Mods in Documents.
				// Redirect them with NTFS junctions (no admin rights needed, unlike
				// symlinks) - tConfig just sees a normal-looking local folder that's
				// actually the shared per-instance save location underneath.
				if (category == GameCategory.TConfig)
					LinkTConfigModFolders(ui, installDir, InstancePaths.GetSaveDataDirForVersion(category, entry.Version));

				string modBuilder = (category == GameCategory.TAPI || category == GameCategory.TConfig)
					? FindModBuilder(installDir, exe) : null;
				if (modBuilder != null)
					ui.AppendLog("Found mod builder: " + modBuilder);

				return (exe, modBuilder);
			}
			catch (OperationCanceledException) {
				TryDelete(archivePath);
				throw;
			}
			catch (Exception ex) {
				ui.AppendLog("Download failed: " + ex.Message);
				TryDelete(archivePath);
				return (null, null);
			}
		}

		// tConfig's own folders for installed mod packs, confirmed by pulling the
		// literal path strings out of tConfig.exe/ModPack Builder.exe directly
		// (both reference "\ModPacks\..." relative to their own directory) - these
		// are created next to the exe on first run if nothing redirects them first.
		// "Config Mod" is deliberately NOT here: tConfig.exe references
		// "Config Mod.ini" (a file, not a folder), so junctioning a same-named
		// directory wouldn't affect it at all - it's a small per-instance settings
		// file, not real save data, and isn't worth chasing further. Also confirmed
		// tConfig.exe has no "-savedirectory" string anywhere in it, meaning
		// (unlike Terraria/tModLoader) it doesn't support that flag at all -
		// Worlds/Players stay hardcoded to the shared Documents\My Games\Terraria
		// and can't be isolated this way; only ModPacks can.
		private static readonly string[] TConfigModFolders = { "ModPacks", "ModPacks_temp_runtime" };

		private static void LinkTConfigModFolders(DownloadProgressWindow ui, string installDir, string saveDir) {
			foreach (string folder in TConfigModFolders) {
				try {
					CreateJunction(Path.Combine(installDir, folder), Path.Combine(saveDir, folder));
				}
				catch (Exception ex) {
					ui.AppendLog("Could not link " + folder + ": " + ex.Message);
				}
			}
		}

		// Creates an NTFS directory junction at linkPath pointing to targetPath,
		// creating targetPath first if needed. Junctions (unlike symlinks) don't
		// require admin rights or Developer Mode on Windows, and to anything
		// reading/writing through linkPath they're indistinguishable from a real
		// folder - which is what lets us redirect tConfig's hardcoded folders
		// without touching tConfig.exe itself.
		internal static void CreateJunction(string linkPath, string targetPath) {
			Directory.CreateDirectory(targetPath);

			if (Directory.Exists(linkPath)) {
				// Could be a leftover real folder from a previous run (or something
				// tConfig's own zip shipped empty). Only safe to replace with a
				// junction if there's nothing in it - never risk deleting user data.
				if (Directory.EnumerateFileSystemEntries(linkPath).Any())
					return;
				Directory.Delete(linkPath);
			}

			var psi = new ProcessStartInfo {
				FileName = "cmd.exe",
				Arguments = "/c mklink /J \"" + linkPath + "\" \"" + targetPath + "\"",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};
			using (Process proc = Process.Start(psi)) {
				proc.WaitForExit();
				if (proc.ExitCode != 0)
					throw new IOException("mklink failed: " + proc.StandardError.ReadToEnd().Trim());
			}
		}

		// Directory.Delete(path, recursive: true) throws UnauthorizedAccessException
		// when the tree contains a junction (like the ones CreateJunction makes for
		// tConfig's mod folders) instead of just unlinking it. Used by RemoveInstance
		// so deleting an install folder never risks touching - or failing on - the
		// real save data a junction inside it points at.
		internal static void DeleteDirectoryTree(string path) {
			if (!Directory.Exists(path)) return;

			foreach (string dir in Directory.GetDirectories(path)) {
				if ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) != 0)
					Directory.Delete(dir, recursive: false);
				else
					DeleteDirectoryTree(dir);
			}
			foreach (string file in Directory.GetFiles(path))
				File.Delete(file);

			Directory.Delete(path, recursive: false);
		}

		// Runs Prism's patcher.exe against the copied Terraria.exe (see the comment
		// where this is called), waiting for Prism.Terraria.dll to appear next to it.
		// patcher.exe is an old, unmaintained third-party tool we don't fully trust
		// to always exit cleanly headless, so this bounds the wait and kills it
		// rather than risking the download hanging forever.
		private static async Task<bool> RunPrismPatcherAsync(DownloadProgressWindow ui, string installDir, CancellationToken ct) {
			string terrariaExe = Path.Combine(installDir, "Terraria.exe");
			string patcherExe = Path.Combine(installDir, "patcher.exe");
			if (!File.Exists(terrariaExe) || !File.Exists(patcherExe)) {
				ui.AppendLog("Missing " + (File.Exists(terrariaExe) ? "patcher.exe" : "Terraria.exe") + " - cannot patch Prism.");
				return false;
			}

			ui.AppendLog("Running Prism's patcher against the copied Terraria.exe...");
			ui.SetProgress(-1);

			var psi = new ProcessStartInfo {
				FileName = patcherExe,
				// Passing the path as an argument (equivalent to dropping the file
				// onto patcher.exe) avoids its interactive "ask for a Terraria.exe" prompt.
				Arguments = "\"" + terrariaExe + "\"",
				WorkingDirectory = installDir,
				UseShellExecute = false,
				CreateNoWindow = true,
			};

			try {
				using (Process proc = Process.Start(psi)) {
					if (proc == null) {
						ui.AppendLog("Failed to start patcher.exe.");
						return false;
					}
					Task exited = proc.WaitForExitAsync(ct);
					Task timeout = Task.Delay(TimeSpan.FromMinutes(3), ct);
					if (await Task.WhenAny(exited, timeout) == timeout) {
						ui.AppendLog("patcher.exe timed out after 3 minutes.");
						try { proc.Kill(entireProcessTree: true); } catch { }
						return false;
					}
				}
			}
			catch (OperationCanceledException) { throw; }
			catch (Exception ex) {
				ui.AppendLog("Failed to run patcher.exe: " + ex.Message);
				return false;
			}

			bool patched = File.Exists(Path.Combine(installDir, "Prism.Terraria.dll"));
			if (!patched)
				ui.AppendLog("patcher.exe exited, but Prism.Terraria.dll was not produced.");
			return patched;
		}

		// Downloads a second archive and extracts it over the base install,
		// overwriting matching files — used for versions distributed as
		// "base build + hotfix/replacement" (see VersionEntry.PatchUrl).
		private static async Task<bool> ApplyPatchAsync(DownloadProgressWindow ui, string patchUrl,
			string installDir, CancellationToken ct) {
			string patchPath = Path.Combine(installDir, "_patch" + ArchiveExtension(patchUrl));
			try {
				ui.AppendLog("Downloading patch " + patchUrl);
				await DownloadFileAsync(patchUrl, patchPath, ui, ct);

				ui.AppendLog("Applying patch...");
				ui.SetProgress(-1);
				await Task.Run(() => Extract(patchPath, installDir), ct);
				File.Delete(patchPath);
				return true;
			}
			catch (OperationCanceledException) {
				TryDelete(patchPath);
				throw;
			}
			catch (Exception ex) {
				ui.AppendLog("Patch failed: " + ex.Message);
				TryDelete(patchPath);
				return false;
			}
		}

		public static async Task DownloadFileAsync(string url, string destPath, DownloadProgressWindow ui,
			CancellationToken ct) {
			using (var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct)) {
				response.EnsureSuccessStatusCode();

				long? total = response.Content.Headers.ContentLength;
				using (var input = await response.Content.ReadAsStreamAsync(ct))
				using (var output = File.Create(destPath)) {
					byte[] buffer = new byte[81920];
					long done = 0;
					long lastReport = 0;
					int read;
					while ((read = await input.ReadAsync(buffer, 0, buffer.Length, ct)) > 0) {
						await output.WriteAsync(buffer, 0, read, ct);
						done += read;
						if (total > 0) {
							ui.SetProgress((double)done / total.Value);
							if (done - lastReport > 5 * 1024 * 1024) {
								lastReport = done;
								ui.AppendLog(string.Format("  {0:F1} / {1:F1} MB", done / 1048576.0, total.Value / 1048576.0));
							}
						}
					}
					if (total > 0)
						ui.AppendLog(string.Format("  {0:F1} MB - done", total.Value / 1048576.0));
				}
			}
		}

		// Categories/types that need a real Terraria install alongside them to
		// behave correctly, rather than as a fully independent standalone game.
		internal static bool NeedsTerrariaBaseCopy(GameCategory category, VersionEntry entry) =>
			category == GameCategory.TConfig
			|| (category == GameCategory.StandAlone
				&& (entry.Type == "Prism" || entry.Type == "Prepare to Die" || entry.Type == "Avalon"));

		// Prefers the exact Terraria version this entry was built against - already
		// downloaded as its own instance via the "Requires Terraria X" dependency
		// prompt in VersionPickerWindow - over whatever's currently Steam-installed.
		// Old builds (tConfig especially, which predates Terraria 1.2's Steam
		// depot/manifest system entirely) expect era-correct native DLLs (XNA/FNA,
		// SDL2, etc.); copying in a modern Terraria's DLLs instead makes them fail
		// to even load at startup rather than just misbehave in-game.
		internal static string FindTerrariaBaseCopySource(VersionEntry entry) {
			if (!string.IsNullOrEmpty(entry.RequiresTerrariaVersion)) {
				string requiredDir = InstancePaths.GetInstallDirForVersion(GameCategory.Terraria, entry.RequiresTerrariaVersion);
				if (File.Exists(Path.Combine(requiredDir, "Terraria.exe")))
					return requiredDir;
			}
			return !string.IsNullOrEmpty(TerrariaLocator.TerrariaPath)
				? Path.GetDirectoryName(TerrariaLocator.TerrariaPath) : null;
		}

		internal static bool IsArchiveUrl(string url) =>
			url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
			|| url.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)
			|| url.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase);

		internal static string ArchiveExtension(string url) {
			if (url.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)) return ".tar.gz";
			if (url.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase)) return ".tgz";
			return ".zip";
		}

		public static void Extract(string archivePath, string destDir) {
			if (archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)
				|| archivePath.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase)) {
				using (var file = File.OpenRead(archivePath))
				using (var gzip = new GZipStream(file, CompressionMode.Decompress))
					System.Formats.Tar.TarFile.ExtractToDirectory(gzip, destDir, overwriteFiles: true);
			}
			else {
				ZipFile.ExtractToDirectory(archivePath, destDir, overwriteFiles: true);
			}
		}

		// Locates the file the launcher should start for a freshly extracted instance.
		public static string FindExecutable(string dir, GameCategory category, string standAloneType = null) {
			foreach (var candidate in CandidateNames(category, standAloneType)) {
				var match = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
					.FirstOrDefault(f => string.Equals(Path.GetFileName(f), candidate, StringComparison.OrdinalIgnoreCase));
				if (match != null) return match;
			}
			// Generic fallback: pick an .exe, preferring one that doesn't look like a server binary.
			return Directory.EnumerateFiles(dir, "*.exe", SearchOption.AllDirectories)
				.OrderBy(f => Path.GetFileName(f).IndexOf("server", StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0)
				.FirstOrDefault();
		}

		// tAPI/tConfig ship a separate GUI tool for packaging mods alongside the
		// main game exe. There's no single well-known filename across every
		// release, so this looks for anything plausibly named for that purpose.
		// Also used by EditGameWindow to detect a builder next to a Custom-linked exe.
		internal static string FindModBuilder(string dir, string mainExe) {
			return Directory.EnumerateFiles(dir, "*.exe", SearchOption.AllDirectories)
				.FirstOrDefault(f => !string.Equals(f, mainExe, StringComparison.OrdinalIgnoreCase)
					&& (Path.GetFileNameWithoutExtension(f).IndexOf("builder", StringComparison.OrdinalIgnoreCase) >= 0
						|| Path.GetFileNameWithoutExtension(f).IndexOf("compiler", StringComparison.OrdinalIgnoreCase) >= 0));
		}

		private static System.Collections.Generic.IEnumerable<string> CandidateNames(GameCategory category, string standAloneType = null) {
			switch (category) {
			case GameCategory.Terraria:
				// Only reached for direct-URL Terraria downloads (pre-Steam-manifest
				// versions) - the Steam path goes through DepotDownloaderService instead.
				yield return "Terraria.exe";
				break;
			case GameCategory.TModLoader:
				yield return "start-tModLoader.bat";
				yield return "tModLoader.exe";
				break;
			case GameCategory.TAPI:
				yield return "tAPI.exe";
				break;
			case GameCategory.TConfig:
				yield return "tConfig.exe";
				break;
			case GameCategory.StandAlone:
				// Avalon ships its own "Avalon 1.1 Remastered.exe" (confirmed across
				// every release, beta v4 through v2.1.0) alongside the copied Steam
				// Terraria files (see NeedsTerrariaBaseCopy) rather than replacing
				// Terraria.exe, so that copied exe must not win here - it's a plain
				// Steam build that fails at startup (missing SteamAPI_Init) when run
				// outside Steam's own launch path.
				if (standAloneType == "Avalon")
					yield return "Avalon 1.1 Remastered.exe";
				yield return "Terraria.exe";
				yield return "TerrariaServer.exe";
				break;
			}
		}

		// Returns the single top-level folder name every entry in a zip is nested
		// under (e.g. "tConfig 0.38"), or null if entries sit at the zip root or
		// span more than one top-level folder. Not attempted for tar.gz archives
		// since nothing currently downloaded that way needs it.
		internal static string GetZipRootFolder(string archivePath) {
			if (!archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
				return null;

			string root = null;
			using (var zip = ZipFile.OpenRead(archivePath)) {
				foreach (var entry in zip.Entries) {
					if (string.IsNullOrEmpty(entry.Name) && entry.FullName.EndsWith("/"))
						continue; // directory-only entry, no bearing on the root name

					int slash = entry.FullName.IndexOf('/');
					if (slash < 0) return null; // a file sits directly at the zip root

					string top = entry.FullName.Substring(0, slash);
					if (root == null) root = top;
					else if (root != top) return null;
				}
			}
			return root;
		}

		// Recursively copies sourceDir's contents into destDir, overwriting any
		// files already there by the same name.
		internal static void CopyDirectory(string sourceDir, string destDir, CancellationToken ct) {
			Directory.CreateDirectory(destDir);
			foreach (string dir in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories)) {
				ct.ThrowIfCancellationRequested();
				Directory.CreateDirectory(Path.Combine(destDir, Path.GetRelativePath(sourceDir, dir)));
			}
			foreach (string file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories)) {
				ct.ThrowIfCancellationRequested();
				string destFile = Path.Combine(destDir, Path.GetRelativePath(sourceDir, file));
				Directory.CreateDirectory(Path.GetDirectoryName(destFile));
				File.Copy(file, destFile, overwrite: true);
			}
		}

		private static void TryDelete(string path) {
			try { if (File.Exists(path)) File.Delete(path); }
			catch { }
		}
	}
}
