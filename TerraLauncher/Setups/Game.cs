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
		// The downloaded version identifier (e.g. "1.4.5.6", "r16") — set when this
		// entry came from the version picker, used to detect "already installed"
		// and to resolve dependencies (e.g. a tAPI build requiring a Terraria version).
		public string Version { get; set; } = "";
		// Kept for the save-folder logic below; derived from Category rather than
		// stored separately so there's a single source of truth for "is this tModLoader".
		public bool IsTMod {
			get { return Category == GameCategory.TModLoader; }
		}
		public override string Arguments {
			get {
				if (SaveDirectory != "Default")
					return "-savedirectory \"" + SaveDirectory + "\"";
				return "";
			}
			set { }
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
				? InstancePaths.GetInstallDirForVersion(Category, Version) : null;
			bool hasManagedFiles = installDir != null && Directory.Exists(installDir);

			string message = hasManagedFiles
				? "Remove \"" + Name + "\" from the list and delete its files?\n\n" + installDir
				: "Remove \"" + Name + "\" from the list?";

			MessageBoxResult result = TriggerMessageBox.Show(Config.MainWindow, MessageIcon.Warning,
				message, "Remove Instance", MessageBoxButton.YesNo);
			if (result != MessageBoxResult.Yes) return;

			Sounds.PlayClose();

			RemoveFromFolder(Config.Games, this);

			if (hasManagedFiles) {
				try { Directory.Delete(installDir, recursive: true); }
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
