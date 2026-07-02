using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TerraLauncher.Windows;

namespace TerraLauncher.Instances;

// HTTP download + archive extraction backend for tModLoader/tAPI/tConfig/StandAlone.
public static class Downloader {
	private static readonly HttpClient _http = new();

	static Downloader() {
		_http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "TerraLauncher/1.0");
		_http.Timeout = TimeSpan.FromMinutes(30);
	}

	// Downloads entry.Url, extracts it into installDir, returns the detected
	// executable path (null on failure).
	public static async Task<string?> InstallFromUrlAsync(DownloadProgressWindow ui, VersionEntry entry,
		InstanceCategory category, string installDir, CancellationToken ct) {
		if (string.IsNullOrEmpty(entry.Url)) {
			ui.AppendLog("No download URL for this version.");
			return null;
		}

		Directory.CreateDirectory(installDir);

		// Direct file (e.g. tAPI installer .exe) — no extraction step
		if (!IsArchiveUrl(entry.Url)) {
			string fileName = Path.GetFileName(new Uri(entry.Url).LocalPath);
			if (string.IsNullOrEmpty(fileName)) fileName = "download.bin";
			string dest = Path.Combine(installDir, fileName);
			try {
				ui.AppendLog($"Downloading {entry.Url}");
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
			ui.AppendLog($"Downloading {entry.Url}");
			await DownloadFileAsync(entry.Url, archivePath, ui, ct);

			ui.AppendLog("Extracting…");
			ui.SetProgress(-1);
			await Task.Run(() => Extract(archivePath, installDir), ct);
			File.Delete(archivePath);

			if (!OperatingSystem.IsWindows())
				MarkExecutables(installDir);

			string? exe = FindExecutable(installDir, category);
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

	public static async Task DownloadFileAsync(string url, string destPath, DownloadProgressWindow ui,
		CancellationToken ct) {
		using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
		response.EnsureSuccessStatusCode();

		long? total = response.Content.Headers.ContentLength;
		await using var input = await response.Content.ReadAsStreamAsync(ct);
		await using var output = File.Create(destPath);

		var buffer = new byte[81920];
		long done = 0;
		long lastReport = 0;
		int read;
		while ((read = await input.ReadAsync(buffer, ct)) > 0) {
			await output.WriteAsync(buffer.AsMemory(0, read), ct);
			done += read;
			if (total > 0) {
				ui.SetProgress((double)done / total.Value);
				if (done - lastReport > 5 * 1024 * 1024) {
					lastReport = done;
					ui.AppendLog($"  {done / 1048576.0:F1} / {total.Value / 1048576.0:F1} MB");
				}
			}
		}
		if (total > 0) ui.AppendLog($"  {total.Value / 1048576.0:F1} MB — done");
	}

	private static bool IsArchiveUrl(string url) =>
		url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
		|| url.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)
		|| url.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase);

	private static string ArchiveExtension(string url) {
		if (url.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)) return ".tar.gz";
		if (url.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase)) return ".tgz";
		return ".zip";
	}

	public static void Extract(string archivePath, string destDir) {
		if (archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)
			|| archivePath.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase)) {
			using var file = File.OpenRead(archivePath);
			using var gzip = new System.IO.Compression.GZipStream(file, CompressionMode.Decompress);
			System.Formats.Tar.TarFile.ExtractToDirectory(gzip, destDir, overwriteFiles: true);
		}
		else {
			ZipFile.ExtractToDirectory(archivePath, destDir, overwriteFiles: true);
		}
	}

	// Zip extraction loses unix permission bits — restore execute on the
	// files that plausibly need it.
	public static void MarkExecutables(string dir) {
		if (OperatingSystem.IsWindows()) return;
		foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)) {
			string name = Path.GetFileName(f);
			bool wantsExec =
				name.EndsWith(".sh", StringComparison.OrdinalIgnoreCase) ||
				name.EndsWith(".command", StringComparison.OrdinalIgnoreCase) ||
				f.Contains(".app/Contents/MacOS/", StringComparison.Ordinal) ||
				!Path.GetExtension(name).Contains('.') ||
				name.Contains(".bin.", StringComparison.OrdinalIgnoreCase);
			if (!wantsExec) continue;
			try {
				File.SetUnixFileMode(f, File.GetUnixFileMode(f)
					| UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
			}
			catch { }
		}
	}

	// Locates the file the launcher should start for a freshly extracted instance.
	public static string? FindExecutable(string dir, InstanceCategory category) {
		// macOS: an .app bundle beats everything (Launch() opens it via `open`)
		if (OperatingSystem.IsMacOS()) {
			var app = Directory.EnumerateDirectories(dir, "*.app", SearchOption.AllDirectories)
				.OrderBy(p => p.Count(c => c == Path.DirectorySeparatorChar))
				.FirstOrDefault();
			if (app != null) return app;
		}

		foreach (var candidate in CandidateNames(category)) {
			var match = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
				.FirstOrDefault(f => string.Equals(Path.GetFileName(f), candidate,
					StringComparison.OrdinalIgnoreCase));
			if (match != null) return match;
		}

		// Generic fallbacks
		if (OperatingSystem.IsWindows()) {
			return Directory.EnumerateFiles(dir, "*.exe", SearchOption.AllDirectories)
				.OrderBy(f => Path.GetFileName(f).Contains("server", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
				.FirstOrDefault();
		}
		return Directory.EnumerateFiles(dir, "*.sh", SearchOption.AllDirectories)
			.OrderBy(f => Path.GetFileName(f).Contains("server", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
			.FirstOrDefault();
	}

	private static IEnumerable<string> CandidateNames(InstanceCategory category) {
		bool win = OperatingSystem.IsWindows();
		switch (category) {
			case InstanceCategory.Terraria:
				if (win) { yield return "Terraria.exe"; }
				else { yield return "Terraria"; yield return "Terraria.bin.x86_64"; yield return "Terraria.bin.osx"; }
				break;
			case InstanceCategory.TModLoader:
				if (win) { yield return "start-tModLoader.bat"; yield return "tModLoader.exe"; }
				else { yield return "start-tModLoader.sh"; yield return "tModLoader"; }
				break;
			case InstanceCategory.TAPI:
				yield return "tAPI.exe";
				break;
			case InstanceCategory.TConfig:
				yield return "tConfig.exe";
				break;
			case InstanceCategory.StandAlone:
				if (win) { yield return "TerrariaServer.exe"; yield return "Terraria.exe"; }
				else { yield return "TerrariaServer"; yield return "TerrariaServer.bin.x86_64"; }
				break;
			case InstanceCategory.Tool:
				break;
		}
	}

	private static void TryDelete(string path) {
		try { if (File.Exists(path)) File.Delete(path); }
		catch { }
	}
}
