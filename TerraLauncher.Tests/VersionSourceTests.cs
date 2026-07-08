using System.Linq;
using TerraLauncher.Instances;
using Xunit;

namespace TerraLauncher.Tests {
	public class VersionSourceTests {
		[Fact]
		public void ParseTerrariaManifestCfg_ParsesValidLines() {
			string cfg = "1.4.5.6,3697031852993618000\n1.4.4.9,2837233928423795000\n";

			var entries = VersionSource.ParseTerrariaManifestCfg(cfg);

			Assert.Equal(2, entries.Count);
			Assert.Equal("1.4.5.6", entries[0].Version);
			Assert.Equal("3697031852993618000", entries[0].ManifestId);
			Assert.Equal("Terraria 1.4.5.6", entries[0].Name);
			Assert.Equal("Re-Logic", entries[0].Author);
			Assert.Equal(105601, entries[0].DepotId);
		}

		[Fact]
		public void ParseTerrariaManifestCfg_SkipsCommentsAndBlankLines() {
			string cfg = "# comment\n\n1.4.5.6,123\n   \n";

			var entries = VersionSource.ParseTerrariaManifestCfg(cfg);

			Assert.Single(entries);
			Assert.Equal("1.4.5.6", entries[0].Version);
		}

		[Fact]
		public void ParseTerrariaManifestCfg_SkipsNonNumericManifestIds() {
			// Pre-Steam-era rows can't be downloaded via DepotDownloader and must be excluded.
			string cfg = "1.0.6.1,not-a-steam-manifest\n1.4.5.6,123456\n";

			var entries = VersionSource.ParseTerrariaManifestCfg(cfg);

			Assert.Single(entries);
			Assert.Equal("1.4.5.6", entries[0].Version);
		}

		[Fact]
		public void ParseTerrariaManifestCfg_SkipsLinesWithoutComma() {
			string cfg = "1.4.5.6\n1.4.4.9,123\n";

			var entries = VersionSource.ParseTerrariaManifestCfg(cfg);

			Assert.Single(entries);
			Assert.Equal("1.4.4.9", entries[0].Version);
		}

		[Fact]
		public void ParseGitHubReleases_SkipsPrereleases() {
			string json = @"[
				{ ""tag_name"": ""v1.4.4"", ""prerelease"": false, ""published_at"": ""2024-01-15T00:00:00Z"", ""assets"": [] },
				{ ""tag_name"": ""v1.4.4-beta"", ""prerelease"": true, ""published_at"": ""2024-01-01T00:00:00Z"", ""assets"": [] }
			]";

			var entries = VersionSource.ParseGitHubReleases(json);

			Assert.Single(entries);
			Assert.Equal("1.4.4", entries[0].Version);
		}

		[Fact]
		public void ParseGitHubReleases_TrimsLeadingVFromTag() {
			string json = @"[{ ""tag_name"": ""v1.4.4"", ""prerelease"": false, ""published_at"": ""2024-01-15T00:00:00Z"", ""assets"": [] }]";

			var entries = VersionSource.ParseGitHubReleases(json);

			Assert.Equal("1.4.4", entries[0].Version);
			Assert.Equal("tModLoader 1.4.4", entries[0].Name);
		}

		[Fact]
		public void ParseGitHubReleases_LegacyZeroXVersionsRequireTerraria1353() {
			string json = @"[{ ""tag_name"": ""v0.11.8.5"", ""prerelease"": false, ""published_at"": ""2019-01-01T00:00:00Z"", ""assets"": [] }]";

			var entries = VersionSource.ParseGitHubReleases(json);

			Assert.Equal("1.3.5.3", entries[0].RequiresTerrariaVersion);
		}

		[Fact]
		public void ParseGitHubReleases_ModernVersionsDoNotRequireTerraria() {
			string json = @"[{ ""tag_name"": ""v1.4.4"", ""prerelease"": false, ""published_at"": ""2024-01-15T00:00:00Z"", ""assets"": [] }]";

			var entries = VersionSource.ParseGitHubReleases(json);

			Assert.Equal("", entries[0].RequiresTerrariaVersion);
		}

		[Fact]
		public void ParseGitHubReleases_PicksWindowsAssetOverOthers() {
			string json = @"[{
				""tag_name"": ""v0.11.8.5"", ""prerelease"": false, ""published_at"": ""2019-01-01T00:00:00Z"",
				""assets"": [
					{ ""name"": ""tModLoader-mac.zip"", ""browser_download_url"": ""https://example.com/mac.zip"" },
					{ ""name"": ""tModLoader-windows.zip"", ""browser_download_url"": ""https://example.com/windows.zip"" },
					{ ""name"": ""tModLoader-linux.zip"", ""browser_download_url"": ""https://example.com/linux.zip"" }
				]
			}]";

			var entries = VersionSource.ParseGitHubReleases(json);

			Assert.Equal("https://example.com/windows.zip", entries[0].Url);
		}

		[Fact]
		public void ParseGitHubReleases_InvalidJsonReturnsEmptyList() {
			var entries = VersionSource.ParseGitHubReleases("not json");

			Assert.Empty(entries);
		}

		[Fact]
		public void TrimBody_TakesFirstLineAndStripsMarkdownHeader() {
			string result = VersionSource.TrimBody("## Changelog\nSecond line");

			Assert.Equal("Changelog", result);
		}

		[Fact]
		public void TrimBody_TruncatesLongLines() {
			string longLine = new string('a', 100);

			string result = VersionSource.TrimBody(longLine);

			Assert.Equal(83, result.Length); // 80 chars + "..."
			Assert.EndsWith("...", result);
		}

		[Fact]
		public void TrimBody_EmptyOrWhitespaceReturnsEmptyString() {
			Assert.Equal("", VersionSource.TrimBody(""));
			Assert.Equal("", VersionSource.TrimBody("   "));
			Assert.Equal("", VersionSource.TrimBody(null));
		}
	}
}
