using System.IO;
using System.IO.Compression;
using TerraLauncher.Instances;
using Xunit;

namespace TerraLauncher.Tests;

public class DownloaderTests {
    // ── IsArchiveUrl ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("https://example.com/release.zip",    true)]
    [InlineData("https://example.com/release.tar.gz", true)]
    [InlineData("https://example.com/release.tgz",    true)]
    [InlineData("https://archive.org/tAPI-versions/r16.exe", false)]
    [InlineData("https://example.com/file.bin",  false)]
    [InlineData("https://example.com/file.7z",   false)]
    public void IsArchiveUrl_DetectsArchivesByExtension(string url, bool expected) {
        Assert.Equal(expected, Downloader.IsArchiveUrl(url));
    }

    [Theory]
    [InlineData("https://example.com/release.tar.gz", ".tar.gz")]
    [InlineData("https://example.com/release.tgz",    ".tgz")]
    [InlineData("https://example.com/release.zip",    ".zip")]
    [InlineData("https://example.com/installer.exe",  ".zip")] // default fallback
    public void ArchiveExtension_ReturnsCorrectSuffix(string url, string expected) {
        Assert.Equal(expected, Downloader.ArchiveExtension(url));
    }

    // ── Extract ───────────────────────────────────────────────────────────

    [Fact]
    public void Extract_Zip_UnpacksContents() {
        using var scope = new TempScope();
        string zipPath = Path.Combine(scope.Dir, "test.zip");
        string outDir  = Path.Combine(scope.Dir, "out");
        Directory.CreateDirectory(outDir);

        using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create)) {
            using var w = new StreamWriter(zip.CreateEntry("hello.txt").Open());
            w.Write("hello");
        }

        Downloader.Extract(zipPath, outDir);

        Assert.True(File.Exists(Path.Combine(outDir, "hello.txt")));
        Assert.Equal("hello", File.ReadAllText(Path.Combine(outDir, "hello.txt")));
    }

    [Fact]
    public void Extract_Zip_OverwritesExistingFiles() {
        using var scope = new TempScope();
        string zipPath = Path.Combine(scope.Dir, "test.zip");
        string outDir  = Path.Combine(scope.Dir, "out");
        Directory.CreateDirectory(outDir);

        File.WriteAllText(Path.Combine(outDir, "file.txt"), "old");

        using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create)) {
            using var w = new StreamWriter(zip.CreateEntry("file.txt").Open());
            w.Write("new");
        }

        Downloader.Extract(zipPath, outDir);

        Assert.Equal("new", File.ReadAllText(Path.Combine(outDir, "file.txt")));
    }

    // ── FindExecutable ────────────────────────────────────────────────────

    [Fact]
    public void FindExecutable_ReturnsNull_ForEmptyDirectory() {
        using var scope = new TempScope();
        Assert.Null(Downloader.FindExecutable(scope.Dir, InstanceCategory.Terraria));
    }

    [Fact]
    public void FindExecutable_FindsTModLoaderStartScript() {
        using var scope = new TempScope();

        string script = OperatingSystem.IsWindows()
            ? Path.Combine(scope.Dir, "start-tModLoader.bat")
            : Path.Combine(scope.Dir, "start-tModLoader.sh");
        File.WriteAllText(script, "");

        string? result = Downloader.FindExecutable(scope.Dir, InstanceCategory.TModLoader);
        Assert.Equal(script, result);
    }

    [Fact]
    public void FindExecutable_FindsTApiExe() {
        if (!OperatingSystem.IsWindows()) return;
        using var scope = new TempScope();

        string exe = Path.Combine(scope.Dir, "tAPI.exe");
        File.WriteAllText(exe, "");

        Assert.Equal(exe, Downloader.FindExecutable(scope.Dir, InstanceCategory.TAPI));
    }

    // Helper ───────────────────────────────────────────────────────────────

    private sealed class TempScope : IDisposable {
        public string Dir { get; } = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        public TempScope() => System.IO.Directory.CreateDirectory(Dir);
        public void Dispose() { try { System.IO.Directory.Delete(Dir, true); } catch { } }
    }
}
