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
	}
}
