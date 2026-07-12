using System;
using System.IO;
using System.IO.Compression;
using TerraLauncher.Instances;
using TerraLauncher.Setups;
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
		public void NeedsTerrariaBaseCopy_TrueForTConfig() {
			Assert.True(Downloader.NeedsTerrariaBaseCopy(GameCategory.TConfig, new VersionEntry()));
		}

		[Theory]
		[InlineData("Prism")]
		[InlineData("Prepare to Die")]
		[InlineData("Avalon")]
		public void NeedsTerrariaBaseCopy_TrueForStandAloneTypesThatNeedIt(string type) {
			Assert.True(Downloader.NeedsTerrariaBaseCopy(GameCategory.StandAlone, new VersionEntry { Type = type }));
		}

		[Theory]
		[InlineData(GameCategory.Terraria)]
		[InlineData(GameCategory.TModLoader)]
		[InlineData(GameCategory.TAPI)]
		public void NeedsTerrariaBaseCopy_FalseForOtherCategories(GameCategory category) {
			Assert.False(Downloader.NeedsTerrariaBaseCopy(category, new VersionEntry()));
		}

		[Theory]
		[InlineData("N Terraria")]
		[InlineData("Ulterraria")]
		[InlineData("")]
		public void NeedsTerrariaBaseCopy_FalseForOtherStandAloneTypes(string type) {
			Assert.False(Downloader.NeedsTerrariaBaseCopy(GameCategory.StandAlone, new VersionEntry { Type = type }));
		}

		[Fact]
		public void FindTerrariaBaseCopySource_PrefersInstalledRequiredVersionOverSteamPath() {
			// InstancePaths roots everything under Config.ConfigPath's directory
			// (the test binary's own bin folder in this process), so this is safe
			// to create/delete without touching a real Documents\...\Terraria -
			// unlike TerrariaLocator.TerrariaPath, which reads the real Steam
			// install and can't be substituted here, so that fallback path isn't
			// covered by this test.
			string requiredDir = TerraLauncher.Instances.InstancePaths.GetInstallDirForVersion(GameCategory.Terraria, "1.1.2");
			Directory.CreateDirectory(requiredDir);
			string exePath = Path.Combine(requiredDir, "Terraria.exe");
			File.WriteAllBytes(exePath, Array.Empty<byte>());
			try {
				string found = Downloader.FindTerrariaBaseCopySource(new VersionEntry { RequiresTerrariaVersion = "1.1.2" });

				Assert.Equal(requiredDir, found);
			}
			finally {
				Directory.Delete(requiredDir, recursive: true);
			}
		}

		[Fact]
		public void FindTerrariaBaseCopySource_IgnoresRequiredVersionWhenNotInstalled() {
			string requiredDir = TerraLauncher.Instances.InstancePaths.GetInstallDirForVersion(GameCategory.Terraria, "1.1.2-not-installed");
			Assert.False(Directory.Exists(requiredDir));

			string found = Downloader.FindTerrariaBaseCopySource(new VersionEntry { RequiresTerrariaVersion = "1.1.2-not-installed" });

			Assert.NotEqual(requiredDir, found);
		}

		[Fact]
		public void CreateJunction_MakesLinkReadThroughToTarget() {
			string root = Path.Combine(Path.GetTempPath(), "TerraLauncherTests_" + Guid.NewGuid());
			string target = Path.Combine(root, "target");
			string link = Path.Combine(root, "installDir", "ModPacks");
			Directory.CreateDirectory(Path.Combine(root, "installDir"));
			try {
				Downloader.CreateJunction(link, target);
				File.WriteAllText(Path.Combine(target, "pack.txt"), "hello");

				Assert.True(Directory.Exists(link));
				Assert.Equal("hello", File.ReadAllText(Path.Combine(link, "pack.txt")));
			}
			finally {
				// Not Directory.Delete(root, recursive: true) - it throws on a tree
				// containing a junction instead of just unlinking it (that's exactly
				// the bug DeleteDirectoryTree exists to avoid).
				Downloader.DeleteDirectoryTree(root);
			}
		}

		[Fact]
		public void CreateJunction_LeavesNonEmptyExistingFolderAlone() {
			string root = Path.Combine(Path.GetTempPath(), "TerraLauncherTests_" + Guid.NewGuid());
			string target = Path.Combine(root, "target");
			string link = Path.Combine(root, "installDir", "ModPacks");
			Directory.CreateDirectory(Path.Combine(root, "installDir"));
			Directory.CreateDirectory(link);
			File.WriteAllText(Path.Combine(link, "existing-pack.txt"), "don't lose me");
			try {
				Downloader.CreateJunction(link, target);

				Assert.True(File.Exists(Path.Combine(link, "existing-pack.txt")));
			}
			finally {
				Directory.Delete(root, recursive: true);
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
