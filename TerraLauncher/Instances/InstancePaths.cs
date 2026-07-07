using System.IO;
using TerraLauncher.Setups;

namespace TerraLauncher.Instances {
	public static class InstancePaths {
		private static string BaseDir => Path.GetDirectoryName(Config.ConfigPath) ?? ".";

		public static string InstancesRoot => Path.Combine(BaseDir, "Instances");
		public static string ToolsRoot => Path.Combine(BaseDir, "Tools");

		public static string GetInstallDir(GameCategory category, string folderName) =>
			Path.Combine(InstancesRoot, category.ToString(), folderName);

		// Same folder-naming rule the version picker uses at download time — kept
		// here so removal (Game.RemoveInstance) can recompute the exact same path
		// without duplicating the naming logic.
		public static string GetInstallDirForVersion(GameCategory category, string version) {
			string safeName = (category.ToString() + "-" + version).Replace(" ", "-").Replace("/", "-");
			return GetInstallDir(category, safeName);
		}
	}
}
