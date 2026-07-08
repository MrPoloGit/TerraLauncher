using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TerraLauncher.Setups;
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
		// returns the detected executable path (null on failure).
		public static async Task<string> InstallFromUrlAsync(DownloadProgressWindow ui, VersionEntry entry,
			GameCategory category, string installDir, CancellationToken ct) {
			if (string.IsNullOrEmpty(entry.Url)) {
				ui.AppendLog("No download URL for this version.");
				return null;
			}

			Directory.CreateDirectory(installDir);

			// Direct file (e.g. an installer .exe) — no extraction step.
			if (!IsArchiveUrl(entry.Url)) {
				string fileName = Path.GetFileName(new Uri(entry.Url).LocalPath);
				if (string.IsNullOrEmpty(fileName)) fileName = "download.bin";
				string dest = Path.Combine(installDir, fileName);
				try {
					ui.AppendLog("Downloading " + entry.Url);
					await DownloadFileAsync(entry.Url, dest, ui, ct);
					ui.AppendLog("Installed: " + dest);
					return dest;
				}
				catch (OperationCanceledException) {
					TryDelete(dest);
					throw;
				}
				catch (Exception ex) {
					ui.AppendLog("Download failed: " + ex.Message);
					TryDelete(dest);
					return null;
				}
			}

			string archivePath = Path.Combine(installDir, "_download" + ArchiveExtension(entry.Url));

			try {
				ui.AppendLog("Downloading " + entry.Url);
				await DownloadFileAsync(entry.Url, archivePath, ui, ct);

				ui.AppendLog("Extracting...");
				ui.SetProgress(-1);
				await Task.Run(() => Extract(archivePath, installDir), ct);
				File.Delete(archivePath);

				if (!string.IsNullOrEmpty(entry.PatchUrl)
					&& !await ApplyPatchAsync(ui, entry.PatchUrl, installDir, ct))
					return null;

				string exe = FindExecutable(installDir, category);
				if (exe == null) {
					ui.AppendLog("Extracted, but no executable found in " + installDir);
					return null;
				}
				ui.AppendLog("Installed: " + exe);
				return exe;
			}
			catch (OperationCanceledException) {
				TryDelete(archivePath);
				throw;
			}
			catch (Exception ex) {
				ui.AppendLog("Download failed: " + ex.Message);
				TryDelete(archivePath);
				return null;
			}
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
		public static string FindExecutable(string dir, GameCategory category) {
			foreach (var candidate in CandidateNames(category)) {
				var match = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
					.FirstOrDefault(f => string.Equals(Path.GetFileName(f), candidate, StringComparison.OrdinalIgnoreCase));
				if (match != null) return match;
			}
			// Generic fallback: pick an .exe, preferring one that doesn't look like a server binary.
			return Directory.EnumerateFiles(dir, "*.exe", SearchOption.AllDirectories)
				.OrderBy(f => Path.GetFileName(f).IndexOf("server", StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0)
				.FirstOrDefault();
		}

		private static System.Collections.Generic.IEnumerable<string> CandidateNames(GameCategory category) {
			switch (category) {
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
				yield return "TerrariaServer.exe";
				break;
			}
		}

		private static void TryDelete(string path) {
			try { if (File.Exists(path)) File.Delete(path); }
			catch { }
		}
	}
}
