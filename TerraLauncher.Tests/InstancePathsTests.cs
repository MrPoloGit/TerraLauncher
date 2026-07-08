using System.IO;
using TerraLauncher.Instances;
using TerraLauncher.Setups;
using Xunit;

namespace TerraLauncher.Tests {
	public class InstancePathsTests {
		[Fact]
		public void GetInstallDir_JoinsRootCategoryAndFolderName() {
			string dir = InstancePaths.GetInstallDir(GameCategory.TModLoader, "MyFolder");

			Assert.Equal(Path.Combine(InstancePaths.InstancesRoot, "TModLoader", "MyFolder"), dir);
		}

		[Fact]
		public void GetInstallDirForVersion_ReplacesSpacesAndSlashes() {
			string dir = InstancePaths.GetInstallDirForVersion(GameCategory.TAPI, "r16 final/candidate");

			string folderName = Path.GetFileName(dir);
			Assert.DoesNotContain(" ", folderName);
			Assert.DoesNotContain("/", folderName);
			Assert.Equal("TAPI-r16-final-candidate", folderName);
		}

		[Fact]
		public void GetInstallDirForVersion_MatchesGetInstallDir() {
			// RemoveInstance must be able to recompute the exact same path the
			// downloader used, without duplicating the naming rule.
			string viaVersion = InstancePaths.GetInstallDirForVersion(GameCategory.Terraria, "1.4.5.6");
			string viaFolder  = InstancePaths.GetInstallDir(GameCategory.Terraria, "Terraria-1.4.5.6");

			Assert.Equal(viaFolder, viaVersion);
		}

		[Fact]
		public void ToolsRoot_IsSiblingOfInstancesRoot() {
			Assert.Equal(Path.GetDirectoryName(InstancePaths.InstancesRoot), Path.GetDirectoryName(InstancePaths.ToolsRoot));
		}
	}
}
