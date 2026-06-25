using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TerraLauncher.Setups;
using TerraLauncher.Util;

namespace TerraLauncher.Instances;

public static class InstanceManager {
	private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };
	private static List<InstanceRecord> _instances = new();

	// Root: .../Steam/steamapps/common/TerraLauncher/Instances/
	public static string InstancesRoot {
		get {
			string terrariaPath = Config.TerrariaExePath;
			if (string.IsNullOrEmpty(terrariaPath))
				terrariaPath = TerrariaLocator.TerrariaPath;

			if (!string.IsNullOrEmpty(terrariaPath)) {
				string? dir    = Path.GetDirectoryName(terrariaPath); // .../common/Terraria[.app]
				string? common = Path.GetDirectoryName(dir);          // .../common
				if (common != null)
					return Path.Combine(common, "TerraLauncher", "Instances");
			}
			// Fallback: next to the config file
			return Path.Combine(
				Path.GetDirectoryName(Config.ConfigPath) ?? ".",
				"TerraLauncher", "Instances"
			);
		}
	}

	public static string ToolsRoot =>
		Path.Combine(Path.GetDirectoryName(InstancesRoot)!, "Tools");

	private static string InstancesJsonPath =>
		Path.Combine(Path.GetDirectoryName(InstancesRoot)!, "instances.json");

	public static IReadOnlyList<InstanceRecord> Instances => _instances;

	public static void Load() {
		try {
			if (!File.Exists(InstancesJsonPath)) return;
			var text = File.ReadAllText(InstancesJsonPath);
			_instances = JsonSerializer.Deserialize<List<InstanceRecord>>(text, _json) ?? new();
		}
		catch {
			_instances = new();
		}
	}

	public static void Save() {
		try {
			string dir = Path.GetDirectoryName(InstancesJsonPath)!;
			Directory.CreateDirectory(dir);
			File.WriteAllText(InstancesJsonPath, JsonSerializer.Serialize(_instances, _json));
		}
		catch { }
	}

	public static void AddInstance(InstanceRecord record) {
		_instances.Add(record);
		Save();
		SyncToConfig(record);
		Config.SaveConfig();
	}

	private static void SyncToConfig(InstanceRecord record) {
		switch (record.Category) {
			case InstanceCategory.Terraria:
			case InstanceCategory.TModLoader:
			case InstanceCategory.TAPI:
			case InstanceCategory.TConfig:
			case InstanceCategory.StandAlone: {
				var game = new Game {
					Name    = record.Name,
					ExePath = record.ExePath,
					Icon    = record.Category == InstanceCategory.TModLoader ? "TMod" : "Tree",
					Details = record.Version
				};
				Config.Games.Entries.Add(game);
				Config.Modified = true;
				break;
			}
			case InstanceCategory.Tool: {
				var tool = new Tool {
					Name    = record.Name,
					ExePath = record.ExePath,
					Details = record.Version
				};
				Config.Tools.Entries.Add(tool);
				Config.Modified = true;
				break;
			}
		}
	}

	public static string GetInstallDir(InstanceCategory category, string folderName) {
		string cat = category switch {
			InstanceCategory.Terraria   => "Terraria",
			InstanceCategory.TModLoader => "tModLoader",
			InstanceCategory.TAPI       => "tAPI",
			InstanceCategory.TConfig    => "tConfig",
			InstanceCategory.StandAlone => "StandAlone",
			InstanceCategory.Tool       => "Tools",
			_                           => "Other"
		};
		return Path.Combine(InstancesRoot, cat, folderName);
	}

	public static void RemoveByExePath(string exePath) {
		if (string.IsNullOrEmpty(exePath)) return;
		if (_instances.RemoveAll(r => r.ExePath == exePath) > 0)
			Save();
	}

	public static InstanceRecord? FindTerrariaVersion(string version) =>
		_instances.Find(i => i.Category == InstanceCategory.Terraria && i.Version == version);
}
