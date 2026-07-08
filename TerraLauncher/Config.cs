using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TerraLauncher.Setups;
using TerraLauncher.Util;

namespace TerraLauncher {
	public enum SetupTypes {
		Game = 0
	}

	public static class Config {

		public const int ConfigVersion = 1;
		public const string ConfigName = "TerraLauncher.xml";
		// Assembly.Location is empty for single-file published apps (the assembly
		// never exists as its own file on disk) — AppContext.BaseDirectory works
		// for both that and the normal multi-file build.
		public static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, ConfigName);

		public static MainWindow MainWindow { get; private set; }

		public static bool CloseOnGameLaunch { get; set; } = true;

		public static bool DisableTransitions { get; set; } = false;
		public static bool Muted { get; set; } = false;
		public static bool Integration { get; set; } = true;
		public static double ScrollSpeed { get; set; } = 1.0;

		public static bool Modified { get; set; } = false;

		public static SetupFolder Games { get; set; } = new SetupFolder("Game List");

		public static bool LoadConfig(MainWindow mainWindow) {
			try {
				MainWindow = mainWindow;

				if (!File.Exists(ConfigPath) && !string.IsNullOrEmpty(TerrariaLocator.TerrariaPath)) {
					string path = TerrariaLocator.TerrariaPath;
					Game game = new Game();
					game.Name = "Terraria";
					game.Icon = "Tree";
					game.ExePath = path;
					
					string version =FileVersionInfo.GetVersionInfo(path).FileVersion.ToString();
					if (!string.IsNullOrEmpty(version))
						game.Details = "v" + version;
					Games.Entries.Add(game);

					SaveConfig();
					return false;
				}

				XmlNode node;
				XmlAttribute attribute;
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
					attribute = node.Attributes["Game"];
					if (attribute != null && bool.TryParse(attribute.InnerText, out boolValue))
						CloseOnGameLaunch = boolValue;
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

				#endregion
				//--------------------------------
				#region Games

				XmlElement gameFolder = doc.SelectSingleNode("TerraLauncher/Games") as XmlElement;
				if (gameFolder != null) {
					Games.Read<Game>(gameFolder);
				}

				#endregion
			}
			catch (Exception) {
				return false;
			}
			return true;
		}

		public static bool SaveConfig() {
			try {
				XmlElement element;
				XmlDocument doc = new XmlDocument();
				doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));

				XmlElement launcher = doc.CreateElement("TerraLauncher");
				doc.AppendChild(launcher);

				XmlElement version = doc.CreateElement("Version");
				version.AppendChild(doc.CreateTextNode(ConfigVersion.ToString()));
				launcher.AppendChild(version);

				#region Settings

				element = doc.CreateElement("CloseOnLaunch");
				element.SetAttribute("Game", CloseOnGameLaunch.ToString());
				launcher.AppendChild(element);
				
				element = doc.CreateElement("DisableTransitions");
				element.AppendChild(doc.CreateTextNode(DisableTransitions.ToString()));
				launcher.AppendChild(element);

				element = doc.CreateElement("Muted");
				element.AppendChild(doc.CreateTextNode(Muted.ToString()));
				launcher.AppendChild(element);

				element = doc.CreateElement("Integration");
				element.AppendChild(doc.CreateTextNode(Integration.ToString()));
				launcher.AppendChild(element);

				element = doc.CreateElement("ScrollSpeed");
				element.AppendChild(doc.CreateTextNode(ScrollSpeed.ToString()));
				launcher.AppendChild(element);

				#endregion
				//--------------------------------
				#region Games

				element = doc.CreateElement("Games");
				Games.Write<Game>(element, doc);
				launcher.AppendChild(element);

				#endregion

				doc.Save(ConfigPath);

				Modified = false;
			}
			catch (Exception) {
				return false;
			}
			return true;
		}

	}
}
