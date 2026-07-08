using System;
using System.IO;
using System.IO.Compression;
using TerraLauncher.Instances;
using Xunit;

namespace TerraLauncher.Tests {
	public class DownloaderTests {
		[Theory]
		[InlineData("https://example.com/build.zip", true)]
		[InlineData("https://example.com/build.tar.gz", true)]
		[InlineData("https://example.com/build.tgz", true)]
		[InlineData("https://example.com/build.exe", false)]
		[InlineData("https://example.com/build.7z", false)]
		public void IsArchiveUrl_OnlyAcceptsZipAndTar(string url, bool expected) {
			Assert.Equal(expected, Downloader.IsArchiveUrl(url));
		}

		[Theory]
		[InlineData("https://example.com/build.tar.gz", ".tar.gz")]
		[InlineData("https://example.com/build.tgz", ".tgz")]
		[InlineData("https://example.com/build.zip", ".zip")]
		public void ArchiveExtension_MatchesUrlSuffix(string url, string expected) {
			Assert.Equal(expected, Downloader.ArchiveExtension(url));
		}

		[Fact]
		public void FindModBuilder_FindsExeWithBuilderInName() {
			string dir = Path.Combine(Path.GetTempPath(), "TerraLauncherTests_" + Guid.NewGuid());
			Directory.CreateDirectory(dir);
			try {
				string mainExe = Path.Combine(dir, "tConfig.exe");
				string builderExe = Path.Combine(dir, "tConfig Mod Builder.exe");
				File.WriteAllBytes(mainExe, Array.Empty<byte>());
				File.WriteAllBytes(builderExe, Array.Empty<byte>());

				string found = Downloader.FindModBuilder(dir, mainExe);

				Assert.Equal(builderExe, found);
			}
			finally {
				Directory.Delete(dir, recursive: true);
			}
		}

		[Fact]
		public void FindModBuilder_FindsCompilerNamedExe() {
			string dir = Path.Combine(Path.GetTempPath(), "TerraLauncherTests_" + Guid.NewGuid());
			Directory.CreateDirectory(dir);
			try {
				string mainExe = Path.Combine(dir, "tAPI.exe");
				string compilerExe = Path.Combine(dir, "ModCompiler.exe");
				File.WriteAllBytes(mainExe, Array.Empty<byte>());
				File.WriteAllBytes(compilerExe, Array.Empty<byte>());

				string found = Downloader.FindModBuilder(dir, mainExe);

				Assert.Equal(compilerExe, found);
			}
			finally {
				Directory.Delete(dir, recursive: true);
			}
		}

		[Fact]
		public void FindModBuilder_ReturnsNullWhenNoBuilderPresent() {
			string dir = Path.Combine(Path.GetTempPath(), "TerraLauncherTests_" + Guid.NewGuid());
			Directory.CreateDirectory(dir);
			try {
				string mainExe = Path.Combine(dir, "SomeGame.exe");
				File.WriteAllBytes(mainExe, Array.Empty<byte>());

				string found = Downloader.FindModBuilder(dir, mainExe);

				Assert.Null(found);
			}
			finally {
				Directory.Delete(dir, recursive: true);
			}
		}

		[Fact]
		public void CopyDirectory_CopiesFilesAndOverwritesExistingOnes() {
			string root = Path.Combine(Path.GetTempPath(), "TerraLauncherTests_" + Guid.NewGuid());
			string source = Path.Combine(root, "source");
			string dest = Path.Combine(root, "dest");
			Directory.CreateDirectory(Path.Combine(source, "Content"));
			Directory.CreateDirectory(dest);
			try {
				File.WriteAllText(Path.Combine(source, "Terraria.exe"), "base game");
				File.WriteAllText(Path.Combine(source, "Content", "Data.dat"), "base data");
				// Pre-existing file in dest with the same name as one in source -
				// mirrors extracting a mod's zip before copying isn't the order
				// used, but confirms the copy itself always overwrites by name.
				File.WriteAllText(Path.Combine(dest, "Terraria.exe"), "old copy");

				Downloader.CopyDirectory(source, dest, System.Threading.CancellationToken.None);

				Assert.Equal("base game", File.ReadAllText(Path.Combine(dest, "Terraria.exe")));
				Assert.Equal("base data", File.ReadAllText(Path.Combine(dest, "Content", "Data.dat")));
			}
			finally {
				Directory.Delete(root, recursive: true);
			}
		}

		[Fact]
		public void GetZipRootFolder_ReturnsSharedTopLevelFolder() {
			string dir = Path.Combine(Path.GetTempPath(), "TerraLauncherTests_" + Guid.NewGuid());
			Directory.CreateDirectory(dir);
			string zipPath = Path.Combine(dir, "tConfig 0.38.zip");
			try {
				using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create)) {
					zip.CreateEntry("tConfig 0.38/tConfig.exe");
					zip.CreateEntry("tConfig 0.38/Content/Data.dat");
				}

				string root = Downloader.GetZipRootFolder(zipPath);

				Assert.Equal("tConfig 0.38", root);
			}
			finally {
				Directory.Delete(dir, recursive: true);
			}
		}

		[Fact]
		public void GetZipRootFolder_ReturnsNullWhenFilesSitAtRoot() {
			string dir = Path.Combine(Path.GetTempPath(), "TerraLauncherTests_" + Guid.NewGuid());
			Directory.CreateDirectory(dir);
			string zipPath = Path.Combine(dir, "flat.zip");
			try {
				using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create)) {
					zip.CreateEntry("Terraria.exe");
					zip.CreateEntry("Content/Data.dat");
				}

				Assert.Null(Downloader.GetZipRootFolder(zipPath));
			}
			finally {
				Directory.Delete(dir, recursive: true);
			}
		}

		[Fact]
		public void GetZipRootFolder_ReturnsNullWhenMultipleTopLevelFolders() {
			string dir = Path.Combine(Path.GetTempPath(), "TerraLauncherTests_" + Guid.NewGuid());
			Directory.CreateDirectory(dir);
			string zipPath = Path.Combine(dir, "multi.zip");
			try {
				using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create)) {
					zip.CreateEntry("FolderA/File1.txt");
					zip.CreateEntry("FolderB/File2.txt");
				}

				Assert.Null(Downloader.GetZipRootFolder(zipPath));
			}
			finally {
				Directory.Delete(dir, recursive: true);
			}
		}

		[Fact]
		public void FindModBuilder_ExcludesTheMainExeItself() {
			string dir = Path.Combine(Path.GetTempPath(), "TerraLauncherTests_" + Guid.NewGuid());
			Directory.CreateDirectory(dir);
			try {
				string mainExe = Path.Combine(dir, "Mod Builder.exe");
				File.WriteAllBytes(mainExe, Array.Empty<byte>());

				string found = Downloader.FindModBuilder(dir, mainExe);

				Assert.Null(found);
			}
			finally {
				Directory.Delete(dir, recursive: true);
			}
		}
	}
}
