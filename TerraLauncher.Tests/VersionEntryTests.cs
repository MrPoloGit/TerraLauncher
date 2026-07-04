using System;
using System.Text.Json;
using TerraLauncher.Instances;
using Xunit;

namespace TerraLauncher.Tests;

public class VersionEntryTests {
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    // ── VersionEntry ──────────────────────────────────────────────────────

    [Fact]
    public void VersionEntry_RoundTripsJson() {
        var entry = new VersionEntry {
            Name        = "Terraria 1.4.5.6",
            Version     = "1.4.5.6",
            Description = "Latest release",
            Date        = "2024-01-01",
            Author      = "Re-Logic",
            Platforms   = ["windows", "mac", "linux"],
            Url         = "https://example.com/terraria.zip",
        };

        var json         = JsonSerializer.Serialize(entry, Options);
        var deserialized = JsonSerializer.Deserialize<VersionEntry>(json, Options);

        Assert.NotNull(deserialized);
        Assert.Equal(entry.Name,        deserialized!.Name);
        Assert.Equal(entry.Version,     deserialized.Version);
        Assert.Equal(entry.Url,         deserialized.Url);
        Assert.Equal(entry.Platforms,   deserialized.Platforms);
    }

    [Fact]
    public void VersionEntry_ManifestIdsCanBeNull() {
        const string json = """{"name":"T","version":"1.0","description":"","date":"","author":"","platforms":[]}""";
        var entry = JsonSerializer.Deserialize<VersionEntry>(json, Options);
        Assert.NotNull(entry);
        Assert.Null(entry!.ManifestIds);
        Assert.Null(entry.DepotIds);
    }

    [Fact]
    public void VersionEntry_DeserializesManifestIds() {
        const string json = """
            {
                "name": "Terraria 1.4.5.6",
                "version": "1.4.5.6",
                "description": "",
                "date": "",
                "author": "",
                "platforms": ["windows"],
                "manifestIds": { "windows": "12345", "mac": null, "linux": null }
            }
            """;
        var entry = JsonSerializer.Deserialize<VersionEntry>(json, Options);
        Assert.NotNull(entry!.ManifestIds);
        Assert.Equal("12345", entry.ManifestIds!.Windows);
        Assert.Null(entry.ManifestIds.Mac);
    }

    // ── InstanceRecord ────────────────────────────────────────────────────

    [Fact]
    public void InstanceRecord_GeneratesUniqueGuids() {
        var r1 = new InstanceRecord();
        var r2 = new InstanceRecord();
        Assert.NotEqual(r1.Id, r2.Id);
        Assert.True(Guid.TryParse(r1.Id, out _));
    }

    [Fact]
    public void InstanceRecord_CategoryRoundTripsJson() {
        var record = new InstanceRecord {
            Name     = "Test",
            Category = InstanceCategory.TModLoader,
            ExePath  = "/path/to/tmod",
        };

        var json       = JsonSerializer.Serialize(record, Options);
        var deserialized = JsonSerializer.Deserialize<InstanceRecord>(json, Options);

        Assert.NotNull(deserialized);
        Assert.Equal(InstanceCategory.TModLoader, deserialized!.Category);
    }

    [Fact]
    public void InstanceRecord_LinkedIdRoundTripsJson() {
        const string linked = "abc-123";
        var record = new InstanceRecord { LinkedTerrariaInstanceId = linked };

        var json         = JsonSerializer.Serialize(record, Options);
        var deserialized = JsonSerializer.Deserialize<InstanceRecord>(json, Options);

        Assert.Equal(linked, deserialized!.LinkedTerrariaInstanceId);
    }

    [Fact]
    public void InstanceRecord_AllCategoriesAreDistinctIntegers() {
        // Validates enum values haven't been accidentally duplicated
        var values = Enum.GetValues<InstanceCategory>();
        Assert.Equal(values.Length, new System.Collections.Generic.HashSet<int>(values.Select(v => (int)v)).Count);
    }
}
