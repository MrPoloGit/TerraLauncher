using TerraLauncher.Instances;
using Xunit;

namespace TerraLauncher.Tests;

public class VersionSourceTests {
    private const string TwoReleasesJson = """
        [
            {
                "tag_name": "v2024.9.1",
                "name": "tModLoader v2024.9.1",
                "published_at": "2024-09-15T12:00:00Z",
                "prerelease": false,
                "body": "# Changelog\nSome changes",
                "assets": [
                    {
                        "name": "tModLoader.zip",
                        "browser_download_url": "https://github.com/tModLoader/tModLoader/releases/download/v2024.9.1/tModLoader.zip"
                    }
                ]
            },
            {
                "tag_name": "v2024.9.2-preview",
                "name": "Preview",
                "published_at": "2024-09-20T12:00:00Z",
                "prerelease": true,
                "body": "",
                "assets": []
            }
        ]
        """;

    // ── ParseGitHubReleases ───────────────────────────────────────────────

    [Fact]
    public void ParseGitHubReleases_ParsesStableRelease() {
        var entries = VersionSource.ParseGitHubReleases(TwoReleasesJson);
        Assert.Single(entries);
        var e = entries[0];
        Assert.Equal("tModLoader 2024.9.1", e.Name);
        Assert.Equal("2024.9.1", e.Version);
        Assert.Equal("2024-09-15", e.Date);
        Assert.Contains("tModLoader.zip", e.Url);
    }

    [Fact]
    public void ParseGitHubReleases_FiltersPrereleases() {
        var entries = VersionSource.ParseGitHubReleases(TwoReleasesJson);
        Assert.All(entries, e => Assert.False(e.Version.Contains("preview", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void ParseGitHubReleases_SetsLegacyRequiresTerrariaVersion() {
        const string json = """
            [{"tag_name":"v0.11.9","name":"","published_at":"2021-01-01T00:00:00Z",
              "prerelease":false,"body":"","assets":[]}]
            """;
        var entries = VersionSource.ParseGitHubReleases(json);
        Assert.Single(entries);
        Assert.Equal("1.3.5.3", entries[0].RequiresTerrariaVersion);
    }

    [Fact]
    public void ParseGitHubReleases_ModernVersionHasNoRequirement() {
        const string json = """
            [{"tag_name":"v2024.9.1","name":"","published_at":"2024-09-15T00:00:00Z",
              "prerelease":false,"body":"","assets":[]}]
            """;
        var entries = VersionSource.ParseGitHubReleases(json);
        Assert.Single(entries);
        Assert.Null(entries[0].RequiresTerrariaVersion);
    }

    [Fact]
    public void ParseGitHubReleases_ReturnsEmptyList_OnInvalidJson() {
        Assert.Empty(VersionSource.ParseGitHubReleases("not json at all"));
    }

    [Fact]
    public void ParseGitHubReleases_ReturnsEmptyList_OnEmptyArray() {
        Assert.Empty(VersionSource.ParseGitHubReleases("[]"));
    }

    // ── TrimBody ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("",                         "")]
    [InlineData("Short description",        "Short description")]
    [InlineData("# Heading\nSecond line",   "Heading")]   // strips leading #
    [InlineData("## Sub\nOther",            "Sub")]
    [InlineData("First line\nSecond line",  "First line")]
    public void TrimBody_ReturnsFirstLineWithHeadingStripped(string input, string expected) {
        Assert.Equal(expected, VersionSource.TrimBody(input));
    }

    [Fact]
    public void TrimBody_TruncatesAt80Chars() {
        string longLine = new string('x', 90);
        string result = VersionSource.TrimBody(longLine);
        Assert.EndsWith("…", result);
        Assert.True(result.Length <= 81); // 80 + ellipsis char
    }

    [Fact]
    public void TrimBody_DoesNotTruncateShortLines() {
        string line = new string('x', 79);
        Assert.Equal(line, VersionSource.TrimBody(line));
    }
}
