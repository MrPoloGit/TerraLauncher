using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using TerraLauncher.Controls.Terraria;
using TerraLauncher.Instances;
using TerraLauncher.Windows;

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
		AddRemoveIcon("Game");
		AddRemoveIcon("Server");
		AddRemoveIcon("Tool");
		// Category icons for downloaded/custom instances. User-provided PNGs in
		// Resources/Icons/AddInstance/ win; otherwise fall back to built-ins.
		AddCategoryIcon("TMod",       "avares://TerraLauncher/Resources/Icons/TreeView/TreeViewGameTMod.png",
			"TModLoader");
		AddCategoryIcon("TAPI",       "avares://TerraLauncher/Resources/Terraria/SetupOptions/SetupOptionHammer.png");
		AddCategoryIcon("TConfig",    "avares://TerraLauncher/Resources/Terraria/SetupOptions/SetupOptionGear.png");
		AddCategoryIcon("StandAlone", "avares://TerraLauncher/Resources/Terraria/SetupIcons/SetupIconTShock.png");
		AddCategoryIcon("Custom",     "avares://TerraLauncher/Resources/Terraria/SetupIcons/SetupIconTool.png");
	}

	private static void AddCategoryIcon(string key, string fallbackUri, string? fileName = null) {
		var bmp = LoadAvaloniaAsset($"avares://TerraLauncher/Resources/Icons/AddInstance/{fileName ?? key}.png")
			?? LoadAvaloniaAsset(fallbackUri);
		if (bmp != null) SetupIcons[key] = bmp;
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

	private static void AddRemoveIcon(string name) {
		var bmp = LoadAvaloniaAsset($"avares://TerraLauncher/Resources/Icons/{name}Remove.png");
		if (bmp != null) SetupOptions[name + "Remove"] = bmp;
	}

	private static void AddIconFromPath(string key, string uri) {
		var bmp = LoadAvaloniaAsset(uri);
		if (bmp != null) SetupIcons[key] = bmp;
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

	// Append a line to TerraLauncher-launch.log next to the config file.
	private static void WriteLog(string line) {
		try {
			string logPath = Path.Combine(
				Path.GetDirectoryName(Config.ConfigPath) ?? ".",
				"TerraLauncher-launch.log");
			File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {line}" + Environment.NewLine);
		}
		catch { }
	}

	public void Launch() {
		Sounds.PlayOpen();
		WriteLog($"=== Launching: {Name} ===");
		WriteLog($"ExePath: {ExePath}");
		try {
			bool isMacBundle = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
				&& ExePath.EndsWith(".app", StringComparison.OrdinalIgnoreCase)
				&& Directory.Exists(ExePath);

			bool exists = isMacBundle ? Directory.Exists(ExePath) : File.Exists(ExePath);
			WriteLog($"Path exists: {exists}  (isMacBundle={isMacBundle})");

			// Validate path before attempting launch
			if (!exists) {
				WriteLog("ABORT: path not found");
				Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () => {
					if (Config.MainWindow == null) return;
					string msg = string.IsNullOrEmpty(ExePath)
						? "No executable path is set for this entry.\n\nUse Edit to set the path."
						: $"Could not find:\n\n{ExePath}\n\nThe path may have moved or been deleted. Use Edit to update it.";
					await TriggerMessageBox.ShowAsync(Config.MainWindow, MessageIcon.Error,
						msg, "Cannot Launch", MsgBoxButton.OK);
				});
				return;
			}

			ProcessStartInfo start;
			if (isMacBundle) {
				// Use ArgumentList so paths with spaces (e.g. "Application Support") are not split
				start = new ProcessStartInfo { FileName = "open", UseShellExecute = false };
				start.ArgumentList.Add(ExePath);
				if (!string.IsNullOrEmpty(Arguments)) {
					start.ArgumentList.Add("--args");
					foreach (var arg in SplitArgs(Arguments))
						start.ArgumentList.Add(arg);
				}
			}
			else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
				&& ExePath.EndsWith(".sh", StringComparison.OrdinalIgnoreCase)) {
				// Shell scripts (e.g. start-tModLoader.sh) must run through bash —
				// shell-execute would hand them to a text editor / Terminal window
				start = new ProcessStartInfo {
					FileName         = "/bin/bash",
					UseShellExecute  = false,
					WorkingDirectory = ExeDirectory,
				};
				start.ArgumentList.Add(ExePath);
				foreach (var arg in SplitArgs(Arguments))
					start.ArgumentList.Add(arg);
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				// Launch via /bin/sh so we can:
				// 1. Redirect game output to /dev/null without a pipe — a pipe would
				//    fill up and block the game, and closing TerraLauncher would send
				//    SIGPIPE and kill the game before its window appears.
				// 2. Set LD_LIBRARY_PATH for the game's bundled native libs.
				// 'exec' replaces the shell with the game so no zombie is left behind.
				// $0 = ExePath, "$@" = any game arguments.
				start = new ProcessStartInfo {
					FileName         = "/bin/sh",
					UseShellExecute  = false,
					WorkingDirectory = ExeDirectory,
				};
				string existing = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? "";
				var ldParts = new List<string> { ExeDirectory };
				string lib   = Path.Combine(ExeDirectory, "lib");
				string lib64 = Path.Combine(ExeDirectory, "lib64");
				if (Directory.Exists(lib))   ldParts.Add(lib);
				if (Directory.Exists(lib64)) ldParts.Add(lib64);
				if (!string.IsNullOrEmpty(existing)) ldParts.Add(existing);
				start.Environment["LD_LIBRARY_PATH"] = string.Join(":", ldParts);
				start.ArgumentList.Add("-c");
				start.ArgumentList.Add("exec \"$0\" \"$@\" >/dev/null 2>&1");
				start.ArgumentList.Add(ExePath);
				foreach (var arg in SplitArgs(Arguments))
					start.ArgumentList.Add(arg);
			}
			else {
				start = new ProcessStartInfo {
					FileName         = ExePath,
					Arguments        = Arguments,
					WindowStyle      = ProcessWindowStyle.Normal,
					UseShellExecute  = true,
					WorkingDirectory = ExeDirectory,
				};
			}

			WriteLog($"Process.Start: FileName={start.FileName}  UseShellExecute={start.UseShellExecute}  Args={start.Arguments}");
			var proc = Process.Start(start);
			WriteLog($"Process.Start returned: {(proc == null ? "null" : $"PID {proc.Id}")}");
			if (proc != null) {
				// Drain redirected streams so the child never blocks on a full pipe buffer.
				if (start.RedirectStandardOutput) {
					proc.OutputDataReceived += (_, _) => { };
					proc.BeginOutputReadLine();
				}
				if (start.RedirectStandardError) {
					proc.ErrorDataReceived += (_, _) => { };
					proc.BeginErrorReadLine();
				}
				ProcessTracker.Track(proc);
			}
			bool close = TypeName switch {
				"Game"   => Config.CloseOnGameLaunch,
				"Server" => Config.CloseOnServerLaunch,
				"Tool"   => Config.CloseOnToolLaunch,
				_ => false
			};
			WriteLog($"CloseOnLaunch={close}");
			if (close) {
				// Wait for the launched process to exit before closing.
				// Steam games use a stub exe that exits within ~2 s once Steam takes over;
				// waiting here ensures the real game window is already spawning by the time
				// the launcher disappears. For non-Steam games we cap the wait at 5 s.
				var procForClose = proc;
				_ = System.Threading.Tasks.Task.Run(async () => {
					if (procForClose != null) {
						using var cts = new System.Threading.CancellationTokenSource(
							TimeSpan.FromSeconds(5));
						try { await procForClose.WaitForExitAsync(cts.Token); }
						catch { }
					}
					WriteLog("Closing launcher after process handoff");
					await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(
						() => Config.MainWindow?.Close());
				});
			}
		}
		catch (Exception ex) {
			WriteLog($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
			Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () => {
				if (Config.MainWindow == null) return;
				await TriggerMessageBox.ShowAsync(Config.MainWindow, MessageIcon.Error,
					$"Failed to launch {Name}:\n\n{ex.Message}\n\nPath: {ExePath}",
					"Launch Failed");
			});
		}
	}

	// Splits a command-line string into tokens, respecting "quoted segments"
	private static List<string> SplitArgs(string args) {
		var result = new List<string>();
		int i = 0;
		while (i < args.Length) {
			while (i < args.Length && args[i] == ' ') i++;
			if (i >= args.Length) break;
			if (args[i] == '"') {
				i++;
				int s = i;
				while (i < args.Length && args[i] != '"') i++;
				result.Add(args[s..i]);
				if (i < args.Length) i++;
			}
			else {
				int s = i;
				while (i < args.Length && args[i] != ' ') i++;
				result.Add(args[s..i]);
			}
		}
		return result;
	}

	public void Delete() {
		if (Config.MainWindow == null) return;
		Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () => {
			var window = Config.MainWindow!;

			// Find this entry's instance record
			var record = InstanceManager.Instances.FirstOrDefault(r => r.ExePath == ExePath);

			// Find instances that depend on this Terraria version
			var dependents = new List<InstanceRecord>();
			if (record?.Category == InstanceCategory.Terraria) {
				dependents = InstanceManager.Instances
					.Where(r => r.LinkedTerrariaInstanceId == record.Id)
					.ToList();
			}

			// Confirm removal
			var result = await TriggerMessageBox.ShowAsync(
				window, MessageIcon.Warning,
				$"Remove \"{Name}\" from the list?",
				"Remove Entry", MsgBoxButton.YesNo);
			if (result != MsgBoxResult.Yes) return;

			// Ask about deleting files
			bool deleteFiles = false;
			if (record != null && !string.IsNullOrEmpty(record.InstallPath)
				&& Directory.Exists(record.InstallPath)) {
				var delResult = await TriggerMessageBox.ShowAsync(
					window, MessageIcon.Warning,
					$"Also delete files from disk?\n\n{record.InstallPath}",
					"Delete Files", MsgBoxButton.YesNo);
				deleteFiles = delResult == MsgBoxResult.Yes;
			}

			// Ask about dependent instances
			bool deleteDependents = false;
			if (dependents.Count > 0) {
				string depList = string.Join("\n", dependents.Select(d => $"• {d.Name} ({d.Version})"));
				var depResult = await TriggerMessageBox.ShowAsync(
					window, MessageIcon.Warning,
					$"The following instances depend on this Terraria version:\n\n{depList}\n\nDelete them too?",
					"Delete Dependents", MsgBoxButton.YesNo);
				deleteDependents = depResult == MsgBoxResult.Yes;
			}

			Sounds.PlayClose();

			// Remove this entry
			static bool RemoveFrom(SetupFolder folder, Setup target) {
				if (folder.Entries.Remove(target)) return true;
				foreach (var e in folder.Entries)
					if (e is SetupFolder sub && RemoveFrom(sub, target)) return true;
				return false;
			}
			RemoveFrom(Config.Games, this);
			RemoveFrom(Config.Servers, this);
			RemoveFrom(Config.Tools, this);
			InstanceManager.RemoveByExePath(ExePath);
			if (deleteFiles && record != null)
				InstanceManager.DeleteInstanceFiles(record);

			// Deleting the auto-detected Steam entries must stick across
			// launches, otherwise the Ensure* methods re-add them.
			string steamPath = !string.IsNullOrEmpty(Config.TerrariaExePath)
				? Config.TerrariaExePath : Util.TerrariaLocator.TerrariaPath;
			if (!string.IsNullOrEmpty(steamPath)
				&& string.Equals(ExePath, steamPath, StringComparison.OrdinalIgnoreCase))
				Config.HideSteamTerraria = true;
			if (!string.IsNullOrEmpty(Util.TerrariaLocator.TModLoaderPath)
				&& string.Equals(ExePath, Util.TerrariaLocator.TModLoaderPath, StringComparison.OrdinalIgnoreCase))
				Config.HideSteamTModLoader = true;

			// Remove dependent entries
			if (deleteDependents) {
				foreach (var dep in dependents) {
					RemoveSetupByExePath(dep.ExePath);
					InstanceManager.RemoveByExePath(dep.ExePath);
					InstanceManager.DeleteInstanceFiles(dep);
				}
			}

			Config.Modified = true;
			Config.SaveConfig();
			window.ReloadSetups();
		});
	}

	private static void RemoveSetupByExePath(string exePath) {
		static bool Remove(SetupFolder folder, string path) {
			var match = folder.Entries.OfType<Setup>()
				.FirstOrDefault(s => s.ExePath == path);
			if (match != null) { folder.Entries.Remove(match); return true; }
			foreach (var e in folder.Entries)
				if (e is SetupFolder sub && Remove(sub, path)) return true;
			return false;
		}
		Remove(Config.Games, exePath);
		Remove(Config.Servers, exePath);
		Remove(Config.Tools, exePath);
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
