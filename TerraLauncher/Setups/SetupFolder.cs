using System;
using System.Collections.Generic;
using System.Xml;
using Avalonia.Media.Imaging;
using TerraLauncher.Controls.Terraria;
using TerraLauncher.Windows;

namespace TerraLauncher.Setups;

public class SetupFolder : ISetup {
	public TerrariaSetupFolder? Entry { get; set; } = null;
	public string Name { get; set; } = "Folder";
	public string Details { get; set; } = "";
	public string Icon { get; set; } = "Folder";
	public SetupFolder? Parent { get; set; } = null;
	public List<ISetup> Entries { get; } = new();

	public SetupFolder() { Name = "Root"; }
	public SetupFolder(string name) { Name = name; }
	public SetupFolder(SetupFolder parent) { Parent = parent; }

	public ISetup Clone() {
		var folder = new SetupFolder();
		folder.Name = Name;
		folder.Icon = Icon;
		foreach (var entry in Entries)
			folder.Entries.Add(entry.Clone());
		return folder;
	}

	public SetupFolder CloneFolder() => (SetupFolder)Clone();

	public void Read<T>(XmlElement folder) where T : Setup {
		Entries.Clear();

		if (Parent != null) {
			var node = folder.SelectSingleNode("Name");
			if (node != null) Name = node.InnerText;
			node = folder.SelectSingleNode("Details");
			if (node != null) Details = node.InnerText;
			node = folder.SelectSingleNode("Icon");
			if (node != null) Icon = node.InnerText;
		}

		foreach (XmlNode folderNode in folder) {
			var element = folderNode as XmlElement;
			if (element == null) continue;
			if (element.Name == "Folder") {
				var subFolder = new SetupFolder(this);
				subFolder.Read<T>(element);
				Entries.Add(subFolder);
			}
			else if (element.Name == typeof(T).Name) {
				var setup = Activator.CreateInstance<T>();
				setup.Read(element);
				Entries.Add(setup);
			}
		}
	}

	public void Write<T>(XmlElement folder, XmlDocument doc) {
		if (Parent != null) {
			void AppendText(string name, string value) {
				var el = doc.CreateElement(name);
				el.AppendChild(doc.CreateTextNode(value));
				folder.AppendChild(el);
			}
			AppendText("Name", Name);
			AppendText("Details", Details);
			AppendText("Icon", Icon);
		}

		foreach (var entry in Entries) {
			if (entry is SetupFolder subFolder) {
				var subEl = doc.CreateElement("Folder");
				subFolder.Write<T>(subEl, doc);
				folder.AppendChild(subEl);
			}
			else if (entry is Setup setup) {
				var setupEl = doc.CreateElement(typeof(T).Name);
				setup.Write(setupEl, doc);
				folder.AppendChild(setupEl);
			}
		}
	}

	public Bitmap? LoadIcon() => Setup.LoadFolderIcon(Icon);

	public void EditFolder() {
		if (Config.MainWindow == null) return;
		Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () => {
			if (await EditFolderWindow.ShowDialogAsync(Config.MainWindow, this)) {
				Entry?.Update();
				Config.Modified = true;
				Config.SaveConfig();
			}
		});
	}
}
