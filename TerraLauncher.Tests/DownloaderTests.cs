using System;
using System.IO;
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
