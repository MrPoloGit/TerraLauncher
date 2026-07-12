using TerraLauncher.Instances;
using TerraLauncher.Setups;
using Xunit;

namespace TerraLauncher.Tests {
	// Only NeedsRedirect is covered here - it's pure version-string logic.
	// RedirectBeforeLaunch/RestoreImmediately/SelfHeal touch the real, hardcoded
	// Documents\My Games\Terraria path with no way to inject a test directory,
	// so exercising them here would risk moving a real save folder on whatever
	// machine runs the test suite.
	public class LegacySaveRedirectTests {
		[Theory]
		[InlineData("1.3.0.7")]
		[InlineData("1.2.4.1")]
		[InlineData("1.1")]
		[InlineData("1.0.6.1")]
		public void NeedsRedirect_TrueForTerrariaOlderThanSaveDirectorySupport(string version) {
			Assert.True(LegacySaveRedirect.NeedsRedirect(GameCategory.Terraria, version));
		}

		[Theory]
		[InlineData("1.3.0.8")]
		[InlineData("1.3.1.1")]
		[InlineData("1.4.4.9")]
		public void NeedsRedirect_FalseForTerrariaAtOrAboveSaveDirectorySupport(string version) {
			Assert.False(LegacySaveRedirect.NeedsRedirect(GameCategory.Terraria, version));
		}

		[Fact]
		public void NeedsRedirect_FalseForBlankVersion() {
			Assert.False(LegacySaveRedirect.NeedsRedirect(GameCategory.Terraria, ""));
		}

		[Theory]
		[InlineData(GameCategory.TModLoader)]
		[InlineData(GameCategory.TAPI)]
		[InlineData(GameCategory.TConfig)]
		[InlineData(GameCategory.StandAlone)]
		[InlineData(GameCategory.Custom)]
		public void NeedsRedirect_FalseForNonTerrariaCategoriesRegardlessOfVersion(GameCategory category) {
			Assert.False(LegacySaveRedirect.NeedsRedirect(category, "1.1"));
		}
	}
}
