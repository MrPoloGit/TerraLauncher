using System.IO;
using TerraLauncher.Setups;

namespace TerraLauncher.Instances {
	public static class InstancePaths {
		private static string BaseDir => Path.GetDirectoryName(Config.ConfigPath) ?? ".";

		public static string InstancesRoot => Path.Combine(BaseDir, "Instances");
		public static string ToolsRoot => Path.Combine(BaseDir, "Tools");

		public static string GetInstallDir(GameCategory category, string folderName) =>
			Path.Combine(InstancesRoot, category.ToString(), folderName);
	}
}
