using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TerraLauncher.Setups;
using TerraLauncher.Windows;

namespace TerraLauncher.Instances {
	// Terraria download backend: fetches DepotDownloader on first use and drives
	// it with Steam credentials, piping Steam Guard codes to stdin.
	public static class DepotDownloaderService {
		private const long TerrariaAppId = 105600;
		private const long TerrariaDepotIdWindows = 105601;

		private static readonly HttpClient http = new HttpClient();

		static DepotDownloaderService() {
			http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "TerraLauncher/1.0");
			http.Timeout = TimeSpan.FromMinutes(30);
		}

		private static string ToolDir => Path.Combine(InstancePaths.ToolsRoot, "DepotDownloader");
		private static string BinaryPath => Path.Combine(ToolDir, "DepotDownloader.exe");

		// ── Bootstrap ──────────────────────────────────────────────────────

		public static async Task<bool> EnsureInstalledAsync(DownloadProgressWindow ui, CancellationToken ct) {
			if (File.Exists(BinaryPath)) return true;

			ui.AppendLog("DepotDownloader not found - fetching latest release from GitHub...");
			string json = await http.GetStringAsync(
				"https://api.github.com/repos/SteamRE/DepotDownloader/releases/latest", ct);

			string assetUrl = null;
			string assetName = null;
			using (var doc = JsonDocument.Parse(json)) {
				foreach (var asset in doc.RootElement.GetProperty("assets").EnumerateArray()) {
					string name = asset.GetProperty("name").GetString() ?? "";
					if (name.Contains("windows", StringComparison.OrdinalIgnoreCase)
						&& name.Contains("x64", StringComparison.OrdinalIgnoreCase)
						&& name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) {
						assetUrl = asset.GetProperty("browser_download_url").GetString();
						assetName = name;
						break;
					}
				}
			}
			if (assetUrl == null) {
				ui.AppendLog("No DepotDownloader build found for windows-x64.");
				return false;
			}

			ui.AppendLog("Downloading " + assetName + "...");
			Directory.CreateDirectory(ToolDir);
			string zipPath = Path.Combine(ToolDir, "_dd.zip");
			await Downloader.DownloadFileAsync(assetUrl, zipPath, ui, ct);

			ui.AppendLog("Extracting DepotDownloader...");
			await Task.Run(() => Downloader.Extract(zipPath, ToolDir), ct);
			File.Delete(zipPath);

			if (!File.Exists(BinaryPath)) {
				ui.AppendLog("Extraction finished but the DepotDownloader binary is missing.");
				return false;
			}
			ui.AppendLog("DepotDownloader ready.");
			return true;
		}

		// ── Terraria download ──────────────────────────────────────────────

		// Returns the path of the launchable executable, or null on failure.
		public static async Task<string> DownloadTerrariaAsync(DownloadProgressWindow ui, VersionEntry entry,
			string installDir, string username, string password, CancellationToken ct) {
			if (!await EnsureInstalledAsync(ui, ct)) return null;

			long depot = entry.DepotId > 0 ? entry.DepotId : TerrariaDepotIdWindows;
			bool hasManifest = !string.IsNullOrEmpty(entry.ManifestId);

			if (!hasManifest && entry.Version != "latest") {
				ui.AppendLog("No Steam manifest ID is known for Terraria " + entry.Version + ".");
				ui.AppendLog("Use \"Terraria (Latest)\" instead, which downloads the current version.");
				return null;
			}

			Directory.CreateDirectory(installDir);

			var args = new System.Collections.Generic.List<string> {
				"-app", TerrariaAppId.ToString(),
				"-depot", depot.ToString(),
				"-username", username,
				"-password", password,
				"-remember-password",
				"-dir", installDir,
			};
			if (hasManifest) {
				args.Add("-manifest");
				args.Add(entry.ManifestId);
			}
			else {
				ui.AppendLog("Downloading the current latest Terraria build from Steam...");
			}

			var start = new ProcessStartInfo {
				FileName               = BinaryPath,
				WorkingDirectory       = ToolDir,
				UseShellExecute        = false,
				RedirectStandardOutput = true,
				RedirectStandardError  = true,
				RedirectStandardInput  = true,
				CreateNoWindow         = true,
			};
			foreach (var a in args) start.ArgumentList.Add(a);

			ui.AppendLog("Running DepotDownloader (app " + TerrariaAppId + ", depot " + depot
				+ (hasManifest ? ", manifest " + entry.ManifestId + ")" : ", latest manifest)"));

			using (var proc = Process.Start(start)) {
				if (proc == null) {
					ui.AppendLog("Failed to start DepotDownloader.");
					return null;
				}

				using (ct.Register(() => { try { proc.Kill(entireProcessTree: true); } catch { } })) {
					Task stderrTask = PumpLinesAsync(proc.StandardError, ui);
					await PumpStdoutAsync(proc, ui, ct);
					await stderrTask;
					await proc.WaitForExitAsync(CancellationToken.None);
				}

				ct.ThrowIfCancellationRequested();

				if (proc.ExitCode != 0) {
					ui.AppendLog("DepotDownloader exited with code " + proc.ExitCode + ".");
					return null;
				}

				string exe = Downloader.FindExecutable(installDir, GameCategory.Terraria);
				if (exe == null) {
					// Terraria.exe has no fixed candidate list in Downloader — check directly.
					string direct = Path.Combine(installDir, "Terraria.exe");
					exe = File.Exists(direct) ? direct : null;
				}
				if (exe == null)
					ui.AppendLog("Download finished, but no Terraria executable was found in " + installDir);
				else
					ui.AppendLog("Installed: " + exe);
				return exe;
			}
		}

		private static async Task PumpLinesAsync(StreamReader reader, DownloadProgressWindow ui) {
			string line;
			while ((line = await reader.ReadLineAsync()) != null)
				if (line.Trim().Length > 0) ui.AppendLog(line);
		}

		// DepotDownloader writes Steam Guard prompts without a trailing newline,
		// so stdout must be read character-wise, not line-wise.
		private static async Task PumpStdoutAsync(Process proc, DownloadProgressWindow ui, CancellationToken ct) {
			StreamReader reader = proc.StandardOutput;
			StringBuilder pending = new StringBuilder();
			char[] buffer = new char[256];
			int read;
			while ((read = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0) {
				for (int i = 0; i < read; i++) {
					char c = buffer[i];
					if (c == '\r') continue;
					if (c == '\n') {
						FlushLine(pending.ToString(), ui);
						pending.Clear();
					}
					else {
						pending.Append(c);
					}
				}

				string tail = pending.ToString();
				if (IsAuthPrompt(tail)) {
					ui.AppendLog(tail);
					pending.Clear();
					string code = Application.Current.Dispatcher.Invoke(() =>
						TextPromptWindow.ShowAsync(ui, "Steam Guard", tail.Trim(), "Code"));
					if (string.IsNullOrEmpty(code)) {
						try { proc.Kill(entireProcessTree: true); } catch { }
						throw new OperationCanceledException();
					}
					await proc.StandardInput.WriteLineAsync(code);
					await proc.StandardInput.FlushAsync();
				}
				ct.ThrowIfCancellationRequested();
			}
			if (pending.Length > 0) FlushLine(pending.ToString(), ui);
		}

		private static readonly Regex percentRegex = new Regex(@"^\s*(\d{1,3}(?:\.\d+)?)%", RegexOptions.Compiled);

		private static void FlushLine(string line, DownloadProgressWindow ui) {
			if (line.Trim().Length == 0) return;
			Match m = percentRegex.Match(line);
			double pct;
			if (m.Success && double.TryParse(m.Groups[1].Value, out pct))
				ui.SetProgress(pct / 100.0);
			ui.AppendLog(line);
		}

		private static bool IsAuthPrompt(string tail) {
			if (!tail.TrimEnd().EndsWith(":")) return false;
			return tail.IndexOf("auth code", StringComparison.OrdinalIgnoreCase) >= 0
				|| tail.IndexOf("authentication code", StringComparison.OrdinalIgnoreCase) >= 0
				|| tail.IndexOf("two-factor", StringComparison.OrdinalIgnoreCase) >= 0
				|| tail.IndexOf("2 factor", StringComparison.OrdinalIgnoreCase) >= 0
				|| tail.IndexOf("Steam Guard", StringComparison.OrdinalIgnoreCase) >= 0;
		}
	}
}
