using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;
using TerraLauncher.Setups;
using TerraLauncher.Util;

namespace TerraLauncher;

public enum SetupTypes {
	Game = 0,
	Server = 1,
	Tool = 2
}

public static class Config {

	public const int ConfigVersion = 1;
	public const string ConfigName = "TerraLauncher.xml";
	public static readonly string ConfigPath = Path.Combine(
		Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".",
		ConfigName
	);

	public static MainWindow? MainWindow { get; set; }

	// Launch behaviour
	public static bool CloseOnGameLaunch { get; set; } = true;
	public static bool CloseOnServerLaunch { get; set; } = false;
	public static bool CloseOnToolLaunch { get; set; } = false;

	// UI settings
	public static bool DisableTransitions { get; set; } = false;
	public static bool Muted { get; set; } = false;
	public static bool Integration { get; set; } = true;
	public static double ScrollSpeed { get; set; } = 1.0;

	// Window geometry (formerly in Properties.Settings)
	public static int WindowWidth { get; set; } = 600;
	public static int WindowHeight { get; set; } = 460;
	public static string CurrentTab { get; set; } = "Game";
	public static int SettingsWidth { get; set; } = 360;
	public static int SettingsHeight { get; set; } = 410;

	public static bool Modified { get; set; } = false;

	// Instances
	public static string TerrariaExePath { get; set; } = "";

	// Set when the user deletes the auto-detected Steam Terraria/tModLoader
	// entries, so they aren't re-added on the next launch.
	public static bool HideSteamTerraria { get; set; } = false;
	public static bool HideSteamTModLoader { get; set; } = false;

	public static SetupFolder Games { get; set; } = new SetupFolder("Game List");
	public static SetupFolder Servers { get; set; } = new SetupFolder("Server List");
	public static SetupFolder Tools { get; set; } = new SetupFolder("Tool List");

	public static bool LoadConfig(MainWindow mainWindow) {
		try {
			MainWindow = mainWindow;

			if (!File.Exists(ConfigPath) && !string.IsNullOrEmpty(TerrariaLocator.TerrariaPath)) {
				string path = TerrariaLocator.TerrariaPath;
				Game game = new Game();
				game.Name = "Terraria";
				game.Icon = "Tree";
				game.ExePath = path;

				string version = FileVersionInfo.GetVersionInfo(path).FileVersion ?? "";
				if (!string.IsNullOrEmpty(version))
					game.Details = "v" + version;
				Games.Entries.Add(game);

				string serverPath = Path.Combine(Path.GetDirectoryName(path) ?? ".", "TerrariaServer.exe");
				if (File.Exists(serverPath)) {
					Server server = new Server();
					server.Name = "Terraria Server";
					server.Icon = "ServerTree";
					server.ExePath = serverPath;
					string sv = FileVersionInfo.GetVersionInfo(serverPath).FileVersion ?? "";
					if (!string.IsNullOrEmpty(sv))
						server.Details = "v" + sv;
					Servers.Entries.Add(server);
				}

				SaveConfig();
				return false;
			}

			if (!File.Exists(ConfigPath))
				return true;

			XmlNode? node;
			XmlDocument doc = new XmlDocument();
			doc.Load(ConfigPath);

			int intValue;
			bool boolValue;
			double doubleValue;

			node = doc.SelectSingleNode("TerraLauncher/Version");
			if (node != null && int.TryParse(node.InnerText, out intValue) && (intValue > ConfigVersion || intValue <= 0))
				return false;

			#region Settings

			node = doc.SelectSingleNode("TerraLauncher/CloseOnLaunch");
			if (node != null) {
				var attr = node.Attributes?["Game"];
				if (attr != null && bool.TryParse(attr.InnerText, out boolValue))
					CloseOnGameLaunch = boolValue;
				attr = node.Attributes?["Server"];
				if (attr != null && bool.TryParse(attr.InnerText, out boolValue))
					CloseOnServerLaunch = boolValue;
				attr = node.Attributes?["Tool"];
				if (attr != null && bool.TryParse(attr.InnerText, out boolValue))
					CloseOnToolLaunch = boolValue;
			}

			node = doc.SelectSingleNode("TerraLauncher/DisableTransitions");
			if (node != null && bool.TryParse(node.InnerText, out boolValue))
				DisableTransitions = boolValue;

			node = doc.SelectSingleNode("TerraLauncher/Muted");
			if (node != null && bool.TryParse(node.InnerText, out boolValue))
				Muted = boolValue;

			node = doc.SelectSingleNode("TerraLauncher/Integration");
			if (node != null && bool.TryParse(node.InnerText, out boolValue))
				Integration = boolValue;

			node = doc.SelectSingleNode("TerraLauncher/ScrollSpeed");
			if (node != null && double.TryParse(node.InnerText, out doubleValue))
				ScrollSpeed = doubleValue;

			node = doc.SelectSingleNode("TerraLauncher/WindowWidth");
			if (node != null && int.TryParse(node.InnerText, out intValue) && intValue >= 560)
				WindowWidth = intValue;

			node = doc.SelectSingleNode("TerraLauncher/WindowHeight");
			if (node != null && int.TryParse(node.InnerText, out intValue) && intValue >= 420)
				WindowHeight = intValue;

			node = doc.SelectSingleNode("TerraLauncher/CurrentTab");
			if (node != null && !string.IsNullOrEmpty(node.InnerText))
				CurrentTab = node.InnerText;

			node = doc.SelectSingleNode("TerraLauncher/SettingsWidth");
			if (node != null && int.TryParse(node.InnerText, out intValue) && intValue >= 360)
				SettingsWidth = intValue;

			node = doc.SelectSingleNode("TerraLauncher/SettingsHeight");
			if (node != null && int.TryParse(node.InnerText, out intValue) && intValue >= 410)
				SettingsHeight = intValue;

			node = doc.SelectSingleNode("TerraLauncher/TerrariaExePath");
			if (node != null && !string.IsNullOrEmpty(node.InnerText))
				TerrariaExePath = node.InnerText;

			node = doc.SelectSingleNode("TerraLauncher/HideSteamTerraria");
			if (node != null && bool.TryParse(node.InnerText, out boolValue))
				HideSteamTerraria = boolValue;

			node = doc.SelectSingleNode("TerraLauncher/HideSteamTModLoader");
			if (node != null && bool.TryParse(node.InnerText, out boolValue))
				HideSteamTModLoader = boolValue;

			#endregion

			XmlElement? gameFolder = doc.SelectSingleNode("TerraLauncher/Games") as XmlElement;
			if (gameFolder != null)
				Games.Read<Game>(gameFolder);

			XmlElement? serverFolder = doc.SelectSingleNode("TerraLauncher/Servers") as XmlElement;
			if (serverFolder != null)
				Servers.Read<Server>(serverFolder);

			XmlElement? toolFolder = doc.SelectSingleNode("TerraLauncher/Tools") as XmlElement;
			if (toolFolder != null)
				Tools.Read<Tool>(toolFolder);
		}
		catch {
			return false;
		}
		return true;
	}

	public static bool SaveConfig() {
		try {
			XmlDocument doc = new XmlDocument();
			doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));

			XmlElement launcher = doc.CreateElement("TerraLauncher");
			doc.AppendChild(launcher);

			void AppendText(string name, string value) {
				var el = doc.CreateElement(name);
				el.AppendChild(doc.CreateTextNode(value));
				launcher.AppendChild(el);
			}

			AppendText("Version", ConfigVersion.ToString());

			var closeEl = doc.CreateElement("CloseOnLaunch");
			closeEl.SetAttribute("Game", CloseOnGameLaunch.ToString());
			closeEl.SetAttribute("Server", CloseOnServerLaunch.ToString());
			closeEl.SetAttribute("Tool", CloseOnToolLaunch.ToString());
			launcher.AppendChild(closeEl);

			AppendText("DisableTransitions", DisableTransitions.ToString());
			AppendText("Muted", Muted.ToString());
			AppendText("Integration", Integration.ToString());
			AppendText("ScrollSpeed", ScrollSpeed.ToString());
			AppendText("WindowWidth", WindowWidth.ToString());
			AppendText("WindowHeight", WindowHeight.ToString());
			AppendText("CurrentTab", CurrentTab);
			AppendText("SettingsWidth", SettingsWidth.ToString());
			AppendText("SettingsHeight", SettingsHeight.ToString());
			AppendText("TerrariaExePath", TerrariaExePath);
			AppendText("HideSteamTerraria", HideSteamTerraria.ToString());
			AppendText("HideSteamTModLoader", HideSteamTModLoader.ToString());

			var gamesEl = doc.CreateElement("Games");
			Games.Write<Game>(gamesEl, doc);
			launcher.AppendChild(gamesEl);

			var serversEl = doc.CreateElement("Servers");
			Servers.Write<Server>(serversEl, doc);
			launcher.AppendChild(serversEl);

			var toolsEl = doc.CreateElement("Tools");
			Tools.Write<Tool>(toolsEl, doc);
			launcher.AppendChild(toolsEl);

			doc.Save(ConfigPath);
			Modified = false;
		}
		catch {
			return false;
		}
		return true;
	}
}
