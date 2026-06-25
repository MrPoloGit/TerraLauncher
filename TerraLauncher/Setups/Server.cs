using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TerraLauncher.Windows;

namespace TerraLauncher.Setups;

public class Server : Setup {
	public override string Arguments { get; set; } = "";
	public string WorldDirectory { get; set; } = "Default";
	public bool IsTMod { get; set; } = false;

	protected override string TypeName => "Server";
	protected override string DefaultIcon => "Server";

	public override SetupOption[] Options {
		get {
			var opts = new List<SetupOption> {
				new("Launch Server", "Launch", Launch),
				new("Open Worlds Folder", "Folder", OpenWorldsFolder),
				new("Open Server Folder", "Home", OpenExeFolder),
				new("Edit Server Setup", "Gear", EditServer),
				new("Remove Entry", "ServerRemove", Delete)
			};
			return opts.ToArray();
		}
	}

	public Server() {
		Name = "New Server";
		Icon = "Server";
	}

	public override ISetup Clone() {
		var s = new Server();
		CloneBase(s);
		s.Arguments = Arguments;
		s.WorldDirectory = WorldDirectory;
		s.IsTMod = IsTMod;
		return s;
	}

	protected override void ReadSetup(XmlElement setup) {
		var node = setup.SelectSingleNode("Arguments");
		if (node != null) Arguments = node.InnerText;

		node = setup.SelectSingleNode("WorldDirectory");
		if (node != null) WorldDirectory = node.InnerText;
		if (WorldDirectory == "") WorldDirectory = "Default";

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
		if (!string.IsNullOrWhiteSpace(Arguments)) AppendText("Arguments", Arguments);
		if (!string.IsNullOrWhiteSpace(WorldDirectory)) AppendText("WorldDirectory", WorldDirectory);
		AppendText("IsTMod", IsTMod.ToString());
	}

	public void OpenWorldsFolder() {
		Sounds.PlayOpen();
		try {
			string path;
			if (WorldDirectory == "Default") {
				path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria");
				if (IsTMod) path = Path.Combine(path, "ModLoader");
				path = Path.Combine(path, "Worlds");
			}
			else {
				path = WorldDirectory;
			}
			if (Directory.Exists(path))
				OpenFolder(path);
		}
		catch { }
	}

	public void EditServer() {
		if (Config.MainWindow == null) return;
		Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () => {
			if (await EditServerWindow.ShowDialogAsync(Config.MainWindow, this)) {
				Entry?.Update();
				Config.Modified = true;
				Config.SaveConfig();
			}
		});
	}
}
