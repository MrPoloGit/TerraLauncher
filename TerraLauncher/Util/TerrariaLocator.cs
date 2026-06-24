using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace TerraLauncher.Util;

public static class TerrariaLocator {
	public static readonly string TerrariaPath;

	static TerrariaLocator() {
		TerrariaPath = FindTerrariaPath() ?? "";
	}

	private static string? FindTerrariaPath() {
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			return FindOnWindows();
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			return FindOnMac();
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			return FindOnLinux();
		return null;
	}

	[SupportedOSPlatform("windows")]
	private static string? FindOnWindows() {
		try {
			// Try registry
			using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
			if (key?.GetValue("SteamPath") is string steamPath) {
				var r = SeekDirectory(steamPath, "Terraria.exe");
				if (r != null) return r;
			}
		}
		catch { }
		// Common Windows Steam paths
		foreach (var drive in new[] { "C", "D", "E" }) {
			var path = SeekDirectory($@"{drive}:\Program Files (x86)\Steam", "Terraria.exe");
			if (path != null) return path;
		}
		return null;
	}

	private static string? FindOnMac() {
		string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		var candidates = new[] {
			Path.Combine(home, "Library", "Application Support", "Steam", "steamapps", "common", "Terraria", "Terraria.app"),
			"/Applications/Terraria.app"
		};
		foreach (var c in candidates)
			if (Directory.Exists(c)) return c;
		return null;
	}

	private static string? FindOnLinux() {
		string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		var candidates = new[] {
			Path.Combine(home, ".steam", "steam", "steamapps", "common", "Terraria", "Terraria"),
			Path.Combine(home, ".local", "share", "Steam", "steamapps", "common", "Terraria", "Terraria"),
			"/usr/games/Terraria"
		};
		foreach (var c in candidates)
			if (File.Exists(c)) return c;
		return null;
	}

	private static string? SeekDirectory(string steamDir, string exeName) {
		if (!Directory.Exists(steamDir)) return null;
		var path = Path.Combine(steamDir, "SteamApps", "Common", "Terraria", exeName);
		if (File.Exists(path)) return path;
		path = Path.Combine(steamDir, "steamapps", "common", "Terraria", exeName);
		if (File.Exists(path)) return path;
		return null;
	}
}
