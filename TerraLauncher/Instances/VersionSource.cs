using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TerraLauncher.Setups;

namespace TerraLauncher.Instances {
	// One page of version results, plus whether another page exists (only ever
	// true for GitHub-backed categories like tModLoader — embedded/local lists
	// are always a single page).
	public class VersionPage {
		public List<VersionEntry> Entries { get; set; } = new List<VersionEntry>();
		public bool HasNextPage { get; set; }
	}

	public static class VersionSource {
		private const int PerPage = 30;

		private static readonly HttpClient http = new HttpClient();
		private static readonly JsonSerializerOptions json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

		static VersionSource() {
			http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "TerraLauncher/1.0");
		}

		public static async Task<VersionPage> GetVersionsAsync(GameCategory category, int page = 1) {
			switch (category) {
			case GameCategory.Terraria:
				return SinglePage(LoadEmbedded("TerraLauncher.Resources.VersionData.terraria-versions.json"));
			case GameCategory.TModLoader:
				return await FetchTModLoaderAsync(page);
			case GameCategory.TAPI:
				return SinglePage(LoadEmbedded("TerraLauncher.Resources.VersionData.tapi.json"));
			case GameCategory.TConfig:
				return SinglePage(LoadEmbedded("TerraLauncher.Resources.VersionData.tconfig.json"));
			case GameCategory.StandAlone:
				return SinglePage(LoadEmbedded("TerraLauncher.Resources.VersionData.standalone.json"));
			default:
				return SinglePage(new List<VersionEntry>());
			}
		}

		private static VersionPage SinglePage(List<VersionEntry> entries) =>
			new VersionPage { Entries = entries, HasNextPage = false };

		private static List<VersionEntry> LoadEmbedded(string resourceName) {
			try {
				using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)) {
					if (stream == null) return new List<VersionEntry>();
					return JsonSerializer.Deserialize<List<VersionEntry>>(stream, json) ?? new List<VersionEntry>();
				}
			}
			catch { return new List<VersionEntry>(); }
		}

		private static async Task<VersionPage> FetchTModLoaderAsync(int page) {
			string url = "https://api.github.com/repos/tModLoader/tModLoader/releases?per_page=" + PerPage + "&page=" + page;
			using (var response = await http.GetAsync(url)) {
				response.EnsureSuccessStatusCode();
				string releasesJson = await response.Content.ReadAsStringAsync();

				bool hasNext = false;
				IEnumerable<string> linkValues;
				if (response.Headers.TryGetValues("Link", out linkValues))
					hasNext = string.Join(",", linkValues).Contains("rel=\"next\"");

				return new VersionPage {
					Entries     = ParseGitHubReleases(releasesJson),
					HasNextPage = hasNext,
				};
			}
		}

		internal static List<VersionEntry> ParseGitHubReleases(string releasesJson) {
			var entries = new List<VersionEntry>();
			try {
				var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(releasesJson, json);
				if (releases == null) return entries;
				foreach (var r in releases) {
					if (r.PreRelease) continue;
					string ver = r.TagName.TrimStart('v');
					var entry = new VersionEntry {
						Name        = "tModLoader " + ver,
						Version     = ver,
						Date        = r.PublishedAt.Length >= 10 ? r.PublishedAt.Substring(0, 10) : r.PublishedAt,
						Author      = "tModLoader Team",
						Description = TrimBody(r.Body),
					};
					entry.Url = PickWindowsAsset(r.Assets);
					// Modern tModLoader (1.4.x) ships as its own standalone Steam app that
					// bundles Terraria content — no separate Terraria install needed.
					// Legacy tModLoader (0.x) is a mod loaded on top of Terraria 1.3.5.3.
					Version parsed;
					if (Version.TryParse(ver, out parsed) && parsed.Major == 0)
						entry.RequiresTerrariaVersion = "1.3.5.3";
					entries.Add(entry);
				}
			}
			catch { }
			return entries;
		}

		// Legacy tModLoader (0.11) ships one archive per OS; modern releases ship a
		// single universal tModLoader.zip.
		private static string PickWindowsAsset(List<GitHubAsset> assets) {
			bool IsArchive(string n) => n.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

			var asset = assets.Find(a => IsArchive(a.Name) && a.Name.IndexOf("windows", StringComparison.OrdinalIgnoreCase) >= 0)
				?? assets.Find(a => string.Equals(a.Name, "tModLoader.zip", StringComparison.OrdinalIgnoreCase))
				?? assets.Find(a => IsArchive(a.Name)
					&& a.Name.IndexOf("example", StringComparison.OrdinalIgnoreCase) < 0
					&& a.Name.IndexOf("mac", StringComparison.OrdinalIgnoreCase) < 0
					&& a.Name.IndexOf("linux", StringComparison.OrdinalIgnoreCase) < 0);
			return asset?.BrowserDownloadUrl;
		}

		internal static string TrimBody(string body) {
			if (string.IsNullOrWhiteSpace(body)) return "";
			string line = body.Split('\n')[0].Trim().TrimStart('#').Trim();
			return line.Length > 80 ? line.Substring(0, 80) + "..." : line;
		}
	}

	internal class GitHubRelease {
		[JsonPropertyName("tag_name")]     public string TagName     { get; set; } = "";
		[JsonPropertyName("name")]         public string Name        { get; set; } = "";
		[JsonPropertyName("published_at")] public string PublishedAt { get; set; } = "";
		[JsonPropertyName("prerelease")]   public bool   PreRelease  { get; set; }
		[JsonPropertyName("body")]         public string Body        { get; set; } = "";
		[JsonPropertyName("assets")]       public List<GitHubAsset> Assets { get; set; } = new List<GitHubAsset>();
	}

	internal class GitHubAsset {
		[JsonPropertyName("name")]                 public string Name               { get; set; } = "";
		[JsonPropertyName("browser_download_url")] public string BrowserDownloadUrl { get; set; } = "";
	}
}
