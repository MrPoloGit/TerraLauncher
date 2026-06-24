using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using TerraLauncher.Controls.Terraria;

namespace TerraLauncher.Setups;

public class SetupOption {
	public string Tooltip;
	public string Icon;
	public Action Action;

	public SetupOption(string tooltip, string icon, Action action) {
		Tooltip = tooltip;
		Icon = icon;
		Action = action;
	}
}

public interface ISetup {
	string Name { get; set; }
	string Details { get; set; }
	string Icon { get; set; }
	ISetup Clone();
	Bitmap? LoadIcon();
}

public abstract class Setup : ISetup {
	public TerrariaSetupEntry? Entry { get; set; } = null;
	public string Name { get; set; } = "";
	public string Icon {
		get => _icon;
		set {
			_icon = value;
			_loadedIcon = null;
		}
	}
	public string Details { get; set; } = "";
	public string ExePath { get; set; } = "";
	public string ExeDirectory => Path.GetDirectoryName(ExePath) ?? "";
	public abstract string Arguments { get; set; }
	protected abstract string TypeName { get; }
	protected abstract string DefaultIcon { get; }
	public abstract SetupOption[] Options { get; }

	public static Dictionary<string, Bitmap> SetupIcons { get; } = new();
	public static Dictionary<string, Bitmap> SetupOptions { get; } = new();

	private string _icon = "";
	protected Bitmap? _loadedIcon;

	static Setup() {
		AddIcon("Tree");
		AddIcon("TreeJungle");
		AddIcon("TreeCorruption");
		AddIcon("TreeCrimson");
		AddIcon("TreeCorruptionHallow");
		AddIcon("TreeCrimsonHallow");
		AddIcon("Server");
		AddIcon("ServerTree");
		AddIcon("ServerTreeJungle");
		AddIcon("TShock");
		AddIcon("Tool");
		AddIcon("Folder");

		AddOptionIcon("Launch");
		AddOptionIcon("Folder");
		AddOptionIcon("Home");
		AddOptionIcon("Hammer");
		AddOptionIcon("Key");
		AddOptionIcon("Gear");
		AddOptionIcon("FolderEnter");
		AddOptionIcon("FolderLeave");
	}

	public abstract ISetup Clone();

	protected void CloneBase(Setup setup) {
		setup.Name = Name;
		setup.Icon = Icon;
		setup.Details = Details;
		setup.ExePath = ExePath;
	}

	private static void AddIcon(string name) {
		var bmp = LoadAvaloniaAsset("avares://TerraLauncher/Resources/Terraria/SetupIcons/SetupIcon" + name + ".png");
		if (bmp != null) SetupIcons[name] = bmp;
	}

	private static void AddOptionIcon(string name) {
		var bmp = LoadAvaloniaAsset("avares://TerraLauncher/Resources/Terraria/SetupOptions/SetupOption" + name + ".png");
		if (bmp != null) SetupOptions[name] = bmp;
	}

	public static Bitmap? LoadAvaloniaAsset(string uri) {
		try {
			using var stream = AssetLoader.Open(new Uri(uri));
			return new Bitmap(stream);
		}
		catch {
			return null;
		}
	}

	public void Read(XmlElement setup) {
		XmlNode? node;
		node = setup.SelectSingleNode("Name");
		if (node != null) Name = node.InnerText;
		node = setup.SelectSingleNode("Details");
		if (node != null) Details = node.InnerText;
		node = setup.SelectSingleNode("Icon");
		if (node != null) Icon = node.InnerText;
		node = setup.SelectSingleNode("ExePath");
		if (node != null) ExePath = node.InnerText;
		ReadSetup(setup);
	}

	public void Write(XmlElement setup, XmlDocument doc) {
		void AppendText(string name, string value) {
			var el = doc.CreateElement(name);
			el.AppendChild(doc.CreateTextNode(value));
			setup.AppendChild(el);
		}
		AppendText("Name", Name);
		AppendText("Details", Details);
		AppendText("Icon", Icon);
		AppendText("ExePath", ExePath);
		WriteSetup(setup, doc);
	}

	protected abstract void WriteSetup(XmlElement setup, XmlDocument doc);
	protected abstract void ReadSetup(XmlElement setup);

	public void Launch() {
		Sounds.PlayOpen();
		try {
			if (File.Exists(ExePath)) {
				var start = new ProcessStartInfo {
					FileName = ExePath,
					Arguments = Arguments,
					WindowStyle = ProcessWindowStyle.Normal,
					CreateNoWindow = true,
					UseShellExecute = true,
					WorkingDirectory = ExeDirectory
				};
				var proc = Process.Start(start);
				bool close = TypeName switch {
					"Game" => Config.CloseOnGameLaunch,
					"Server" => Config.CloseOnServerLaunch,
					"Tool" => Config.CloseOnToolLaunch,
					_ => false
				};
				bool ctrl = false, shift = false;

				if (proc != null && (close || ctrl) && !shift)
					Config.MainWindow?.Close();
			}
		}
		catch { }
	}

	public void OpenExeFolder() {
		Sounds.PlayOpen();
		try {
			if (Directory.Exists(ExeDirectory))
				OpenFolder(ExeDirectory);
		}
		catch { }
	}

	protected static void OpenFolder(string path) {
		try {
			Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
		}
		catch { }
	}

	public virtual Bitmap? LoadIcon() {
		if (_loadedIcon == null)
			_loadedIcon = LoadIconBitmap(Icon, DefaultIcon);
		return _loadedIcon;
	}

	public static Bitmap? GetOptionIcon(string name) {
		SetupOptions.TryGetValue(name, out var bmp);
		return bmp;
	}

	public static Bitmap? LoadIconBitmap(string icon, string defaultIcon = "Tree") {
		if (string.IsNullOrWhiteSpace(icon)) {
			SetupIcons.TryGetValue(defaultIcon, out var def);
			return def;
		}
		if (SetupIcons.TryGetValue(icon, out var known))
			return known;
		try {
			return LoadIconFromFile(icon);
		}
		catch {
			SetupIcons.TryGetValue(defaultIcon, out var fallback);
			return fallback;
		}
	}

	public static Bitmap? LoadFolderIcon(string icon) {
		if (string.IsNullOrWhiteSpace(icon)) {
			SetupIcons.TryGetValue("Folder", out var def);
			return def;
		}
		if (SetupIcons.TryGetValue(icon, out var known))
			return known;
		try {
			return LoadIconFromFile(icon);
		}
		catch {
			SetupIcons.TryGetValue("Folder", out var fallback);
			return fallback;
		}
	}

	private static Bitmap? LoadIconFromFile(string filePath) {
		string ext = Path.GetExtension(filePath).ToLower();
		if (ext == ".exe")
			return null;
		// PNG, BMP, JPG, ICO etc. — try Avalonia's decoder
		try { return new Bitmap(filePath); }
		catch { return null; }
	}
}
