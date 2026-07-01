using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TerraLauncher.Instances;

public static class VersionSource {
	private static readonly HttpClient _http = new();
	private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

	static VersionSource() {
		_http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "TerraLauncher/1.0");
	}

	// URLs for user-hosted version lists (update when you set up hosting)
	public const string TApiVersionsUrl       = "https://TODO_YOUR_HOST/tapi-versions.json";
	public const string TConfigVersionsUrl    = "https://TODO_YOUR_HOST/tconfig-versions.json";
	public const string StandAloneVersionsUrl = "https://TODO_YOUR_HOST/standalone-versions.json";

	private static string CacheDir => Path.Combine(
		Path.GetDirectoryName(Config.ConfigPath) ?? ".", "cache");

	public static async Task<List<VersionEntry>> GetVersionsAsync(InstanceCategory category) {
		return category switch {
			InstanceCategory.Terraria   => LoadEmbedded("TerraLauncher.Resources.VersionData.terraria-versions.json"),
			InstanceCategory.TModLoader => await FetchTModLoaderAsync(),
			InstanceCategory.TAPI       => await FetchOrEmbedded(TApiVersionsUrl,       "tapi-cache.json",       "TerraLauncher.Resources.VersionData.tapi.json"),
			InstanceCategory.TConfig    => await FetchOrEmbedded(TConfigVersionsUrl,    "tconfig-cache.json",    "TerraLauncher.Resources.VersionData.tconfig.json"),
			InstanceCategory.StandAlone => await FetchOrEmbedded(StandAloneVersionsUrl, "standalone-cache.json", "TerraLauncher.Resources.VersionData.standalone.json"),
			_                           => new List<VersionEntry>()
		};
	}

	private static List<VersionEntry> LoadEmbedded(string resourceName) {
		try {
			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
			if (stream == null) return new List<VersionEntry>();
			return JsonSerializer.Deserialize<List<VersionEntry>>(stream, _json) ?? new List<VersionEntry>();
		}
		catch { return new List<VersionEntry>(); }
	}

	private static async Task<List<VersionEntry>> FetchOrEmbedded(string url, string cacheFile, string embeddedFallback) {
		string cachePath = Path.Combine(CacheDir, cacheFile);
		if (!url.StartsWith("https://TODO")) {
			try {
				var json = await _http.GetStringAsync(url);
				Directory.CreateDirectory(CacheDir);
				File.WriteAllText(cachePath, json);
				return JsonSerializer.Deserialize<List<VersionEntry>>(json, _json) ?? new List<VersionEntry>();
			}
			catch { }
		}
		if (File.Exists(cachePath)) {
			try { return JsonSerializer.Deserialize<List<VersionEntry>>(File.ReadAllText(cachePath), _json) ?? new List<VersionEntry>(); }
			catch { }
		}
		return LoadEmbedded(embeddedFallback);
	}

	private static async Task<List<VersionEntry>> FetchTModLoaderAsync() {
		string cachePath = Path.Combine(CacheDir, "tmodloader-releases.json");
		string json;
		try {
			json = await _http.GetStringAsync("https://api.github.com/repos/tModLoader/tModLoader/releases?per_page=30");
			Directory.CreateDirectory(CacheDir);
			File.WriteAllText(cachePath, json);
		}
		catch {
			if (!File.Exists(cachePath)) return new List<VersionEntry>();
			json = File.ReadAllText(cachePath);
		}
		return ParseGitHubReleases(json);
	}

	private static List<VersionEntry> ParseGitHubReleases(string json) {
		var entries = new List<VersionEntry>();
		try {
			var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(json, _json);
			if (releases == null) return entries;
			foreach (var r in releases) {
				if (r.PreRelease) continue;
				string ver = r.TagName.TrimStart('v');
				var entry = new VersionEntry {
					Name        = "tModLoader " + ver,
					Version     = ver,
					Date        = r.PublishedAt.Length >= 10 ? r.PublishedAt[..10] : r.PublishedAt,
					Platforms   = ["windows", "mac", "linux"],
					Author      = "tModLoader Team",
					Description = TrimBody(r.Body),
				};
				entry.Url = PickAssetForCurrentOS(r.Assets);
				if (System.Version.TryParse(ver, out var v) && v.Major == 0)
					entry.RequiresTerrariaVersion = "1.3.5.3";
				entries.Add(entry);
			}
		}
		catch { }
		return entries;
	}

	// Legacy tModLoader (0.11) ships one archive per OS; modern releases ship a
	// single universal tModLoader.zip. Prefer the archive for the running OS.
	private static string? PickAssetForCurrentOS(List<GitHubAsset> assets) {
		string os = OperatingSystem.IsWindows() ? "windows"
			: OperatingSystem.IsMacOS() ? "mac" : "linux";
		bool IsArchive(string n) =>
			n.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
			|| n.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase);

		var asset = assets.Find(a => IsArchive(a.Name) && a.Name.Contains(os, StringComparison.OrdinalIgnoreCase))
			?? assets.Find(a => string.Equals(a.Name, "tModLoader.zip", StringComparison.OrdinalIgnoreCase))
			?? assets.Find(a => IsArchive(a.Name)
				&& !a.Name.Contains("example", StringComparison.OrdinalIgnoreCase)
				&& !a.Name.Contains("windows", StringComparison.OrdinalIgnoreCase)
				&& !a.Name.Contains("mac", StringComparison.OrdinalIgnoreCase)
				&& !a.Name.Contains("linux", StringComparison.OrdinalIgnoreCase));
		return asset?.BrowserDownloadUrl;
	}

	private static string TrimBody(string body) {
		if (string.IsNullOrWhiteSpace(body)) return "";
		var line = body.Split('\n')[0].Trim().TrimStart('#').Trim();
		return line.Length > 80 ? line[..80] + "…" : line;
	}
}

internal class GitHubRelease {
	[JsonPropertyName("tag_name")]     public string TagName     { get; set; } = "";
	[JsonPropertyName("name")]         public string Name        { get; set; } = "";
	[JsonPropertyName("published_at")] public string PublishedAt { get; set; } = "";
	[JsonPropertyName("prerelease")]   public bool   PreRelease  { get; set; }
	[JsonPropertyName("body")]         public string Body        { get; set; } = "";
	[JsonPropertyName("assets")]       public List<GitHubAsset> Assets { get; set; } = new();
}

internal class GitHubAsset {
	[JsonPropertyName("name")]                 public string Name               { get; set; } = "";
	[JsonPropertyName("browser_download_url")] public string BrowserDownloadUrl { get; set; } = "";
}
