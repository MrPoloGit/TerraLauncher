using System.IO;
using TerraLauncher.Instances;
using Xunit;

namespace TerraLauncher.Tests;

// Each test gets a fresh temp directory and resets InstanceManager state via Load().
// Config.TerrariaExePath is pointed at a fake Steam path so InstancesRoot is predictable.
public class InstanceManagerTests : IDisposable {
    private readonly string _tempBase;
    private readonly string _savedTerrariaPath;

    public InstanceManagerTests() {
        _savedTerrariaPath = Config.TerrariaExePath;

        _tempBase = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        // Fake: .../steamapps/common/Terraria/Terraria(.exe)
        // InstancesRoot will be .../steamapps/common/TerraLauncher/Instances
        string exe = Path.Combine(_tempBase, "steamapps", "common", "Terraria",
            OperatingSystem.IsWindows() ? "Terraria.exe" : "Terraria");
        Directory.CreateDirectory(Path.GetDirectoryName(exe)!);
        File.WriteAllText(exe, "");

        Config.TerrariaExePath = exe;
        Config.Games  = new Setups.SetupFolder("Game List");
        Config.Tools  = new Setups.SetupFolder("Tool List");

        // Load() returns early (without clearing _instances) when the file
        // doesn't exist, so writing an empty array ensures a clean slate.
        string jsonDir = Path.GetDirectoryName(InstanceManager.InstancesRoot)!;
        Directory.CreateDirectory(jsonDir);
        File.WriteAllText(Path.Combine(jsonDir, "instances.json"), "[]");
        InstanceManager.Load();
    }

    public void Dispose() {
        Config.TerrariaExePath = _savedTerrariaPath;
        try { Directory.Delete(_tempBase, true); } catch { }
    }

    [Fact]
    public void AddInstance_CanBeReloadedFromDisk() {
        var record = new InstanceRecord {
            Name        = "tModLoader 2024.9.1",
            Version     = "2024.9.1",
            Category    = InstanceCategory.TModLoader,
            InstallPath = Path.Combine(InstanceManager.InstancesRoot, "tModLoader", "2024.9.1"),
            ExePath     = Path.Combine(InstanceManager.InstancesRoot, "tModLoader", "2024.9.1", "start-tModLoader.sh"),
        };

        InstanceManager.AddInstance(record);
        InstanceManager.Load(); // reload from disk

        Assert.Single(InstanceManager.Instances);
        var loaded = InstanceManager.Instances[0];
        Assert.Equal("tModLoader 2024.9.1", loaded.Name);
        Assert.Equal("2024.9.1", loaded.Version);
        Assert.Equal(InstanceCategory.TModLoader, loaded.Category);
    }

    [Fact]
    public void AddInstance_PreservesLinkedTerrariaId() {
        var terraria = new InstanceRecord { Name = "Terraria 1.3.5.3", Version = "1.3.5.3", Category = InstanceCategory.Terraria };
        var tapi = new InstanceRecord {
            Name = "tAPI r16", Version = "r16",
            Category = InstanceCategory.TAPI,
            LinkedTerrariaInstanceId = terraria.Id,
        };

        InstanceManager.AddInstance(terraria);
        InstanceManager.AddInstance(tapi);
        InstanceManager.Load();

        var loadedTapi = Assert.Single(InstanceManager.Instances, r => r.Category == InstanceCategory.TAPI);
        Assert.Equal(terraria.Id, loadedTapi.LinkedTerrariaInstanceId);
    }

    [Fact]
    public void RemoveByExePath_RemovesOnlyMatchingRecord() {
        string exeA = "/fake/a.exe";
        string exeB = "/fake/b.exe";

        InstanceManager.AddInstance(new InstanceRecord { Name = "A", ExePath = exeA, Category = InstanceCategory.Terraria });
        InstanceManager.AddInstance(new InstanceRecord { Name = "B", ExePath = exeB, Category = InstanceCategory.Terraria });

        InstanceManager.RemoveByExePath(exeA);

        Assert.Single(InstanceManager.Instances);
        Assert.Equal("B", InstanceManager.Instances[0].Name);
    }

    [Fact]
    public void RemoveByExePath_IgnoresUnknownPaths() {
        InstanceManager.AddInstance(new InstanceRecord { Name = "A", ExePath = "/fake/a.exe", Category = InstanceCategory.Terraria });
        InstanceManager.RemoveByExePath("/does/not/exist.exe");
        Assert.Single(InstanceManager.Instances);
    }

    [Fact]
    public void FindTerrariaVersion_ReturnsMatchingRecord() {
        InstanceManager.AddInstance(new InstanceRecord { Name = "Terraria 1.4.5.6", Version = "1.4.5.6", Category = InstanceCategory.Terraria });
        InstanceManager.AddInstance(new InstanceRecord { Name = "Terraria 1.3.5.3", Version = "1.3.5.3", Category = InstanceCategory.Terraria });

        var found = InstanceManager.FindTerrariaVersion("1.4.5.6");
        Assert.NotNull(found);
        Assert.Equal("Terraria 1.4.5.6", found!.Name);
    }

    [Fact]
    public void FindTerrariaVersion_ReturnsNull_WhenNotFound() {
        Assert.Null(InstanceManager.FindTerrariaVersion("9.9.9.9"));
    }

    [Fact]
    public void GetInstallDir_IncludesCategorySubfolder() {
        string dir = InstanceManager.GetInstallDir(InstanceCategory.TModLoader, "2024.9.1");
        Assert.Contains("tModLoader", dir);
        Assert.Contains("2024.9.1", dir);
        Assert.StartsWith(InstanceManager.InstancesRoot, dir);
    }
}
