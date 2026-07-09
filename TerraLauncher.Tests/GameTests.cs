using TerraLauncher.Setups;
using Xunit;

namespace TerraLauncher.Tests {
	public class GameTests {
		[Fact]
		public void BuildLaunchArguments_DefaultSaveDirectory_IsEmpty() {
			Assert.Equal("", Game.BuildLaunchArguments(GameCategory.Terraria, "Default"));
		}

		[Fact]
		public void BuildLaunchArguments_TerrariaCustomSaveDirectory_PassesSaveDirectoryOnly() {
			string args = Game.BuildLaunchArguments(GameCategory.Terraria, @"C:\Save");

			Assert.Equal("-savedirectory \"C:\\Save\"", args);
		}

		[Fact]
		public void BuildLaunchArguments_TModLoaderCustomSaveDirectory_PassesBothSaveDirectoryFlags() {
			// tModLoader appends its own "tModLoader" subfolder onto -savedirectory
			// rather than treating it as the final save location, so -tmlsavedirectory
			// (which does point directly at the final folder) must also be passed -
			// otherwise Mods/ModConfigs would land one level deeper than intended.
			string args = Game.BuildLaunchArguments(GameCategory.TModLoader, @"C:\Save");

			Assert.Equal("-savedirectory \"C:\\Save\" -tmlsavedirectory \"C:\\Save\"", args);
		}

		[Fact]
		public void BuildLaunchArguments_TConfigCustomSaveDirectory_PassesSaveDirectoryOnly() {
			string args = Game.BuildLaunchArguments(GameCategory.TConfig, @"C:\Save");

			Assert.Equal("-savedirectory \"C:\\Save\"", args);
		}
	}
}
