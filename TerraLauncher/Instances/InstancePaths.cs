using System;
using System.IO;
using TerraLauncher.Setups;

namespace TerraLauncher.Instances {
	public static class InstancePaths {
		private static string BaseDir => Path.GetDirectoryName(Config.ConfigPath) ?? ".";

		public static string InstancesRoot => Path.Combine(BaseDir, "Instances");
		public static string ToolsRoot => Path.Combine(BaseDir, "Tools");

		// Per-instance save data (Worlds/Players/Mods) lives under Documents instead
		// of next to the installed game files, mirroring how Terraria itself splits
		// Steam's install directory from Documents/My Games/Terraria - this keeps
		// each downloaded version's saves isolated from the others.
		private static string SaveDataRoot => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "TerraLauncher", "Instances");

		// Display/folder casing for each category - deliberately not category.ToString()
		// (which would give "StandAlone", "TAPI", "TModLoader") since that doesn't match
		// how these are branded elsewhere in the app (tAPI, tConfig, tModloader, stand-alone).
		public static string FolderName(GameCategory category) {
			switch (category) {
			case GameCategory.Terraria:   return "Terraria";
			case GameCategory.TModLoader: return "tModloader";
			case GameCategory.TAPI:       return "tAPI";
			case GameCategory.TConfig:    return "tConfig";
			case GameCategory.StandAlone: return "stand-alone";
			case GameCategory.Custom:     return "Custom";
			default:                      return category.ToString();
			}
		}

		public static string GetInstallDir(GameCategory category, string folderName) =>
			Path.Combine(InstancesRoot, FolderName(category), folderName);

		public static string GetSaveDataDir(GameCategory category, string folderName) =>
			Path.Combine(SaveDataRoot, FolderName(category), folderName);

		// Same folder-naming rule the version picker uses at download time — kept
		// here so removal (Game.RemoveInstance) can recompute the exact same path
		// without duplicating the naming logic. StandAlone bundles several unrelated
		// games under one category (Avalon, N Terraria, ...), so those pass their
		// VersionEntry.Type as label (e.g. "Avalon-2.1.0") instead of the generic
		// "stand-alone-2.1.0" every version would otherwise collapse into.
		public static string GetInstallDirForVersion(GameCategory category, string version, string label = null) {
			string prefix = string.IsNullOrEmpty(label) ? FolderName(category) : label;
			string safeName = (prefix + "-" + version).Replace(" ", "-").Replace("/", "-");
			return GetInstallDir(category, safeName);
		}

		public static string GetSaveDataDirForVersion(GameCategory category, string version, string label = null) {
			string prefix = string.IsNullOrEmpty(label) ? FolderName(category) : label;
			string safeName = (prefix + "-" + version).Replace(" ", "-").Replace("/", "-");
			return GetSaveDataDir(category, safeName);
		}
	}
}
