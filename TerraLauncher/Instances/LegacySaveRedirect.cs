using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TerraLauncher.Setups;

namespace TerraLauncher.Instances {
	// Terraria only gained "-savedirectory" in Desktop 1.3.0.8 - anything older
	// always reads/writes the real Documents\My Games\Terraria, ignoring the flag
	// entirely (same problem as tConfig, see NeedsTerrariaBaseCopy's neighbor
	// comments in Downloader.cs). If a newer version (or vanilla Steam Terraria)
	// already wrote a config there, launching one of these old builds hits
	// Terraria's own "Older game versions cannot use current game configuration"
	// dialog and refuses to start.
	//
	// To give these old builds isolated saves like everything else, we swap the
	// real folder out for a junction to the instance's own save folder for the
	// duration of the run, then swap it back. Game.Launch() is fire-and-forget
	// (it can even close the whole app right after Process.Start if "Close
	// Launcher on Game Launch" is on), so the restore can't just be "await the
	// process and swap back in the same method call" - it's driven by
	// Process.Exited when the app stays alive, and by SelfHeal() on the next
	// TerraLauncher startup as a fallback for when it doesn't.
	public static class LegacySaveRedirect {
		private static readonly Version MinSaveDirectoryVersion = new Version(1, 3, 0, 8);

		private static string StateRoot => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "TerraLauncher");

		private static string RealSaveDir => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria");

		private static string BackupDir => Path.Combine(StateRoot, "_LegacyTerrariaBackup");
		private static string MarkerPath => Path.Combine(StateRoot, "_LegacyTerrariaBackup.marker");

		// True only for a real Terraria build older than 1.3.0.8 - every other
		// category either supports -savedirectory or (tConfig) is handled by its
		// own junction-based redirect already.
		public static bool NeedsRedirect(GameCategory category, string version) {
			if (category != GameCategory.Terraria)
				return false;
			return TryParseVersion(version, out Version parsed) && parsed < MinSaveDirectoryVersion;
		}

		private static bool TryParseVersion(string version, out Version parsed) {
			parsed = null;
			if (string.IsNullOrWhiteSpace(version))
				return false;
			// Version strings here come from the Steam manifest DB ("1.4.4.9",
			// "1.2.4.1", "1.1", ...) - pad to 4 parts since System.Version requires
			// at least two and treats missing trailing parts as -1, not 0.
			string[] parts = version.Split('.');
			int[] nums = new int[4];
			for (int i = 0; i < 4; i++) {
				if (i >= parts.Length || !int.TryParse(parts[i], out nums[i]))
					nums[i] = 0;
			}
			parsed = new Version(nums[0], nums[1], nums[2], nums[3]);
			return true;
		}

		// Must run before Process.Start for an old-version launch.
		public static void RedirectBeforeLaunch(string instanceSaveDir) {
			SelfHeal();
			if (File.Exists(MarkerPath)) {
				throw new InvalidOperationException(
					"Another pre-1.3.0.8 Terraria instance is still running and using the shared save " +
					"folder (Documents\\My Games\\Terraria). Close it before launching a different old version.");
			}

			Directory.CreateDirectory(instanceSaveDir);
			Directory.CreateDirectory(StateRoot);

			if (Directory.Exists(RealSaveDir)) {
				if (Directory.Exists(BackupDir)) {
					throw new InvalidOperationException(
						"A previous Terraria save backup already exists at " + BackupDir +
						" - restore it to Documents\\My Games\\Terraria manually before launching.");
				}
				Directory.Move(RealSaveDir, BackupDir);
			}

			Downloader.CreateJunction(RealSaveDir, instanceSaveDir);
			File.WriteAllLines(MarkerPath, new[] { instanceSaveDir, "" });
		}

		// Must run right after Process.Start succeeds for an old-version launch,
		// so the marker records the PID SelfHeal() checks on next startup.
		public static void AttachRestore(Process proc) {
			if (proc == null) {
				RestoreImmediately();
				return;
			}
			try {
				if (File.Exists(MarkerPath))
					File.WriteAllLines(MarkerPath, new[] { File.ReadAllLines(MarkerPath)[0], proc.Id.ToString() });
				proc.EnableRaisingEvents = true;
				proc.Exited += (s, e) => RestoreImmediately();
			}
			catch {
				// Process may have already exited, or event hookup was denied -
				// SelfHeal() on next startup is the fallback for this case.
			}
		}

		public static void RestoreImmediately() {
			if (!File.Exists(MarkerPath))
				return;
			try {
				if (IsJunction(RealSaveDir))
					Directory.Delete(RealSaveDir); // removes only the reparse point, not the target's contents
				if (Directory.Exists(BackupDir))
					Directory.Move(BackupDir, RealSaveDir);
			}
			finally {
				TryDeleteMarker();
			}
		}

		// Called on every TerraLauncher startup, and before starting a new
		// redirect, to recover from a previous run that closed (via "Close
		// Launcher on Game Launch") before Process.Exited could fire.
		public static void SelfHeal() {
			if (!File.Exists(MarkerPath))
				return;

			string[] lines;
			try { lines = File.ReadAllLines(MarkerPath); }
			catch { return; }

			int pid;
			bool stillRunning = lines.Length > 1 && int.TryParse(lines[1], out pid) && IsProcessRunning(pid);
			if (stillRunning)
				return; // that old instance is still using the redirect - leave it alone

			RestoreImmediately();
		}

		private static bool IsProcessRunning(int pid) {
			try {
				return !Process.GetProcessById(pid).HasExited;
			}
			catch (ArgumentException) {
				return false;
			}
		}

		private static bool IsJunction(string path) =>
			Directory.Exists(path) && (new DirectoryInfo(path).Attributes & FileAttributes.ReparsePoint) != 0;

		private static void TryDeleteMarker() {
			try { File.Delete(MarkerPath); }
			catch { }
		}
	}
}
