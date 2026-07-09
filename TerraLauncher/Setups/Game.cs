using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using TerraLauncher.Controls.Terraria;
using TerraLauncher.Instances;
using TerraLauncher.Windows;

namespace TerraLauncher.Setups {
	public enum GameCategory {
		Terraria = 0,
		TModLoader = 1,
		TAPI = 2,
		TConfig = 3,
		StandAlone = 4,
		Custom = 5
	}

	public class Game : Setup {
		//========== PROPERTIES ==========
		#region Properties

		public string SaveDirectory { get; set; } = "Default";
		public GameCategory Category { get; set; } = GameCategory.Terraria;
		// Path to a bundled mod-packaging tool, if one was found alongside the
		// exe - populated by the downloader for tAPI/tConfig, and re-detected by
		// EditGameWindow whenever the exe path is set (including Custom links).
		public string ModBuilderPath { get; set; } = "";
		// The downloaded version identifier (e.g. "1.4.5.6", "r16") — set when this
		// entry came from the version picker, used to detect "already installed"
		// and to resolve dependencies (e.g. a tAPI build requiring a Terraria version).
		public string Version { get; set; } = "";
		// VersionEntry.Type for StandAlone downloads (e.g. "Avalon", "N Terraria") —
		// blank for everything else. Kept so RemoveInstance can recompute the exact
		// install/save-data folder names, which are keyed off this instead of the
		// generic "StandAlone" category name.
		public string SubType { get; set; } = "";
		// Kept for the save-folder logic below; derived from Category rather than
		// stored separately so there's a single source of truth for "is this tModLoader".
		public bool IsTMod {
			get { return Category == GameCategory.TModLoader; }
		}
		public override string Arguments {
			get { return BuildLaunchArguments(Category, SaveDirectory); }
			set { }
		}

		// Split out from the Arguments getter so it's testable without needing a
		// real Game (constructing one touches Setup's static ctor, which loads
		// pack:// icon URIs and needs a live WPF Application to resolve).
		internal static string BuildLaunchArguments(GameCategory category, string saveDirectory) {
			if (saveDirectory == "Default")
				return "";

			// tModLoader ignores -savedirectory for its own data: per its source
			// (Program.TML.cs, SetSavePath()), it takes whatever -savedirectory
			// resolved to and appends its own "tModLoader" subfolder onto it, so
			// Mods/ModConfigs/ModSources would land one level deeper than every
			// other category's SaveDirectory. -tmlsavedirectory is tModLoader's
			// own flag for exactly this - it points AT the final save folder
			// directly (no subfolder appended), so Worlds/Players/Mods all end up
			// together in our per-instance folder like everywhere else. Passing
			// both keeps this working on older tModLoader builds that might
			// predate -tmlsavedirectory and only look for -savedirectory.
			if (category == GameCategory.TModLoader)
				return "-savedirectory \"" + saveDirectory + "\" -tmlsavedirectory \"" + saveDirectory + "\"";

			return "-savedirectory \"" + saveDirectory + "\"";
		}
		protected override string TypeName {
			get { return "Game"; }
		}
		protected override string DefaultIcon {
			get { return "Tree"; }
		}
		public override SetupOption[] Options {
			get {
				List<SetupOption> options = new List<SetupOption>();
				options.Add(new SetupOption("Launch Game", "Launch", Launch));
				if (!string.IsNullOrEmpty(ModBuilderPath))
					options.Add(new SetupOption("Launch Mod Builder", "Wrench", LaunchModBuilder));
				options.Add(new SetupOption("Open Save Folder", "Folder", OpenSaveFolder));
				options.Add(new SetupOption("Open Executable Folder", "Home", OpenExeFolder));
				options.Add(new SetupOption("Edit Game Setup", "Gear", EditGame));
				options.Add(new SetupOption("Remove Instance", "GameRemove", RemoveInstance));
				return options.ToArray();
			}
		}

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		public Game() {
			Name = "New Game";
			Icon = "Tree";
		}
		public override ISetup Clone() {
			Game game = new Game();
			CloneBase(game);
			game.SaveDirectory = SaveDirectory;
			game.Category = Category;
			game.Version = Version;
			game.ModBuilderPath = ModBuilderPath;
			game.SubType = SubType;
			return game;
		}

		#endregion
		//=========== LOADING ============
		#region Loading

		protected override void ReadSetup(XmlElement setup) {
			XmlNode node;

			bool boolValue;

			node = setup.SelectSingleNode("SaveDirectory");
			if (node != null) {
				SaveDirectory = node.InnerText;
			}
			if (SaveDirectory == "")
				SaveDirectory = "Default";

			node = setup.SelectSingleNode("Category");
			GameCategory categoryValue;
			if (node != null && Enum.TryParse(node.InnerText, out categoryValue)) {
				Category = categoryValue;
			}
			else {
				// Legacy config: only IsTMod was saved.
				node = setup.SelectSingleNode("IsTMod");
				if (node != null && bool.TryParse(node.InnerText, out boolValue) && boolValue)
					Category = GameCategory.TModLoader;
			}

			node = setup.SelectSingleNode("Version");
			if (node != null) Version = node.InnerText;

			node = setup.SelectSingleNode("ModBuilderPath");
			if (node != null) ModBuilderPath = node.InnerText;

			node = setup.SelectSingleNode("SubType");
			if (node != null) SubType = node.InnerText;
		}
		protected override void WriteSetup(XmlElement setup, XmlDocument doc) {
			XmlElement element;

			element = doc.CreateElement("SaveDirectory");
			element.AppendChild(doc.CreateTextNode(SaveDirectory));
			setup.AppendChild(element);

			element = doc.CreateElement("Category");
			element.AppendChild(doc.CreateTextNode(Category.ToString()));
			setup.AppendChild(element);

			element = doc.CreateElement("Version");
			element.AppendChild(doc.CreateTextNode(Version));
			setup.AppendChild(element);

			element = doc.CreateElement("ModBuilderPath");
			element.AppendChild(doc.CreateTextNode(ModBuilderPath));
			setup.AppendChild(element);

			element = doc.CreateElement("SubType");
			element.AppendChild(doc.CreateTextNode(SubType));
			setup.AppendChild(element);
		}

		#endregion
		//=========== OPTIONS ============
		#region Options

		public void OpenSaveFolder() {
			Sounds.PlayOpen();
			if (SaveDirectory == "Default") {
				string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria");
				if (IsTMod)
					path = Path.Combine(path, "ModLoader");
				if (Directory.Exists(path))
					OpenFolder(path);
			}
			else if (Directory.Exists(SaveDirectory)) {
				OpenFolder(SaveDirectory);
			}
		}
		public void LaunchModBuilder() {
			Sounds.PlayOpen();
			if (!File.Exists(ModBuilderPath)) {
				TriggerMessageBox.Show(Config.MainWindow, MessageIcon.Error,
					"Could not find the Mod Builder:\n\n" + ModBuilderPath + "\n\nThe path may have moved or been deleted.",
					"Cannot Launch");
				return;
			}
			try {
				Process.Start(new ProcessStartInfo(ModBuilderPath) {
					UseShellExecute = true,
					WorkingDirectory = Path.GetDirectoryName(ModBuilderPath)
				});
			}
			catch (Exception ex) {
				TriggerMessageBox.Show(Config.MainWindow, MessageIcon.Error,
					"Failed to launch Mod Builder:\n\n" + ex.Message, "Launch Failed");
			}
		}
		public void EditGame() {
			if (EditGameWindow.ShowDialog(Config.MainWindow, this)) {
				Entry?.Update();
				Config.Modified = true;
				Config.SaveConfig();
			}
		}

		public void RemoveInstance() {
			if (Config.MainWindow == null) return;

			// Only versions that came from the downloader own an install folder —
			// Steam auto-detected entries and Custom-linked executables point at
			// files this app doesn't manage, so those must never be deleted.
			string installDir = !string.IsNullOrEmpty(Version)
				? InstancePaths.GetInstallDirForVersion(Category, Version, SubType) : null;
			bool hasManagedFiles = installDir != null && Directory.Exists(installDir);

			// Worlds/Players/Mods live separately from the install dir (see
			// InstancePaths.GetSaveDataDir) and are asked about independently below,
			// since losing saves is a much bigger deal than losing the game files.
			bool hasSaveData = SaveDirectory != "Default" && Directory.Exists(SaveDirectory);

			string message = hasManagedFiles
				? "Remove \"" + Name + "\" from the list and delete its files?\n\n" + installDir
				: "Remove \"" + Name + "\" from the list?";

			MessageBoxResult result = TriggerMessageBox.Show(Config.MainWindow, MessageIcon.Warning,
				message, "Remove Instance", MessageBoxButton.YesNo);
			if (result != MessageBoxResult.Yes) return;

			bool deleteSaveData = false;
			if (hasSaveData) {
				MessageBoxResult saveResult = TriggerMessageBox.Show(Config.MainWindow, MessageIcon.Warning,
					"Also delete its Worlds, Players, and Mods?\n\n" + SaveDirectory
						+ "\n\nChoose No to keep your saves and just remove the instance.",
					"Delete Save Data", MessageBoxButton.YesNo);
				deleteSaveData = saveResult == MessageBoxResult.Yes;
			}

			Sounds.PlayClose();

			RemoveFromFolder(Config.Games, this);

			if (hasManagedFiles) {
				// Not a plain Directory.Delete(..., recursive: true): tConfig installs
				// contain junctions into the save-data folder (see Downloader.
				// CreateJunction), which that throws on instead of just unlinking.
				try { Downloader.DeleteDirectoryTree(installDir); }
				catch { }
			}
			if (deleteSaveData) {
				try { Directory.Delete(SaveDirectory, recursive: true); }
				catch { }
			}

			Config.Modified = true;
			Config.SaveConfig();
			Config.MainWindow.ReloadSetups();
		}

		private static bool RemoveFromFolder(SetupFolder folder, Setup target) {
			if (folder.Entries.Remove(target)) return true;
			foreach (var entry in folder.Entries) {
				if (entry is SetupFolder sub && RemoveFromFolder(sub, target)) return true;
			}
			return false;
		}

		#endregion
	}
}
