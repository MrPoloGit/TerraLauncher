using System.IO;
using TerraLauncher.Instances;
using TerraLauncher.Setups;
using Xunit;

namespace TerraLauncher.Tests {
	public class InstancePathsTests {
		[Fact]
		public void GetInstallDir_JoinsRootCategoryAndFolderName() {
			string dir = InstancePaths.GetInstallDir(GameCategory.TModLoader, "MyFolder");

			Assert.Equal(Path.Combine(InstancePaths.InstancesRoot, "tModloader", "MyFolder"), dir);
		}

		[Fact]
		public void GetInstallDirForVersion_ReplacesSpacesAndSlashes() {
			string dir = InstancePaths.GetInstallDirForVersion(GameCategory.TAPI, "r16 final/candidate");

			string folderName = Path.GetFileName(dir);
			Assert.DoesNotContain(" ", folderName);
			Assert.DoesNotContain("/", folderName);
			Assert.Equal("tAPI-r16-final-candidate", folderName);
		}

		[Theory]
		[InlineData(GameCategory.Terraria, "Terraria")]
		[InlineData(GameCategory.TModLoader, "tModloader")]
		[InlineData(GameCategory.TAPI, "tAPI")]
		[InlineData(GameCategory.TConfig, "tConfig")]
		[InlineData(GameCategory.StandAlone, "stand-alone")]
		public void FolderName_MatchesExpectedCasing(GameCategory category, string expected) {
			Assert.Equal(expected, InstancePaths.FolderName(category));
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

		[Fact]
		public void GetInstallDirForVersion_UsesLabelInsteadOfCategoryWhenGiven() {
			string dir = InstancePaths.GetInstallDirForVersion(GameCategory.StandAlone, "2.1.0", "Avalon");

			string folderName = Path.GetFileName(dir);
			Assert.Equal("Avalon-2.1.0", folderName);
			// Still nested under the category-level directory.
			Assert.Equal("stand-alone", Path.GetFileName(Path.GetDirectoryName(dir)));
		}

		[Fact]
		public void GetInstallDirForVersion_FallsBackToCategoryWhenLabelMissing() {
			string dir = InstancePaths.GetInstallDirForVersion(GameCategory.StandAlone, "2.1.0");

			Assert.Equal("stand-alone-2.1.0", Path.GetFileName(dir));
		}

		[Fact]
		public void GetSaveDataDirForVersion_UsesLabelInsteadOfCategoryWhenGiven() {
			string dir = InstancePaths.GetSaveDataDirForVersion(GameCategory.StandAlone, "2.1.0", "N Terraria");

			Assert.Equal("N-Terraria-2.1.0", Path.GetFileName(dir));
		}
	}
}
