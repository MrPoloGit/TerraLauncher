using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Avalonia.Media.Imaging;
using TerraLauncher.Windows;

namespace TerraLauncher.Setups;

public class Game : Setup {
	public string SaveDirectory { get; set; } = "Default";
	public bool IsTMod { get; set; } = false;

	public override string Arguments {
		get => SaveDirectory != "Default" ? $"-savedirectory \"{SaveDirectory}\"" : "";
		set { }
	}
	protected override string TypeName => "Game";
	protected override string DefaultIcon => "Tree";

	public override SetupOption[] Options {
		get {
			var opts = new List<SetupOption> {
				new("Launch Game", "Launch", Launch),
				new("Open Save Folder", "Folder", OpenSaveFolder),
				new("Open Executable Folder", "Home", OpenExeFolder),
				new("Edit Game Setup", "Gear", EditGame)
			};
			return opts.ToArray();
		}
	}

	public Game() {
		Name = "New Game";
		Icon = "Tree";
	}

	public override ISetup Clone() {
		var g = new Game();
		CloneBase(g);
		g.SaveDirectory = SaveDirectory;
		g.IsTMod = IsTMod;
		return g;
	}

	protected override void ReadSetup(XmlElement setup) {
		var node = setup.SelectSingleNode("SaveDirectory");
		if (node != null) SaveDirectory = node.InnerText;
		if (SaveDirectory == "") SaveDirectory = "Default";

		node = setup.SelectSingleNode("IsTMod");
		if (node != null && bool.TryParse(node.InnerText, out bool b))
			IsTMod = b;
	}

	protected override void WriteSetup(XmlElement setup, XmlDocument doc) {
		void AppendText(string name, string value) {
			var el = doc.CreateElement(name);
			el.AppendChild(doc.CreateTextNode(value));
			setup.AppendChild(el);
		}
		AppendText("SaveDirectory", SaveDirectory);
		AppendText("IsTMod", IsTMod.ToString());
	}

	public void OpenSaveFolder() {
		Sounds.PlayOpen();
		try {
			string path;
			if (SaveDirectory == "Default") {
				path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria");
				if (IsTMod) path = Path.Combine(path, "ModLoader");
			}
			else {
				path = SaveDirectory;
			}
			if (Directory.Exists(path))
				OpenFolder(path);
		}
		catch { }
	}

	public void EditGame() {
		if (Config.MainWindow == null) return;
		// Must be called from UI thread; uses async dispatch
		Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () => {
			if (await EditGameWindow.ShowDialogAsync(Config.MainWindow, this)) {
				Entry?.Update();
				Config.Modified = true;
				Config.SaveConfig();
			}
		});
	}
}
