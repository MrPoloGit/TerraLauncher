using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using TerraLauncher.Controls.Terraria;
using TerraLauncher.Instances;
using TerraLauncher.Setups;
using TerraLauncher.Util;
using TerraLauncher.Windows;

namespace TerraLauncher;

public partial class MainWindow : Window {
	private bool _closing;
	private readonly Stack<TerrariaSetupList> _gameStack = new();
	private readonly Stack<TerrariaSetupList> _serverStack = new();
	private readonly Stack<TerrariaSetupList> _toolStack = new();
	private SetupTypes _currentTab = SetupTypes.Game;

	private Stack<TerrariaSetupList> CurrentStack => _currentTab switch {
		SetupTypes.Game => _gameStack,
		SetupTypes.Server => _serverStack,
		_ => _toolStack
	};
	private SetupFolder CurrentFolder => _currentTab switch {
		SetupTypes.Game => Config.Games,
		SetupTypes.Server => Config.Servers,
		_ => Config.Tools
	};
	private Grid CurrentGrid => _currentTab switch {
		SetupTypes.Game => gridGames,
		SetupTypes.Server => gridServers,
		_ => gridTools
	};

	public MainWindow() {
		InitializeComponent();
		Config.MainWindow = this;
		LoadSettings();
		InstanceManager.Load();

		Opened += async (_, _) => {
			Sounds.PlayOpen();
			await FadeAsync(0, 1, 0.4);
		};

		Closing += OnWindowClosing;
		KeyDown += OnKeyDown;
	}

	private void LoadSettings() {
		Config.LoadConfig(this);
		EnsureSteamTerrariaEntry();
		LoadSetups();

		if (Config.WindowWidth >= MinWidth) Width = Config.WindowWidth;
		if (Config.WindowHeight >= MinHeight) Height = Config.WindowHeight;

		if (Enum.TryParse<SetupTypes>(Config.CurrentTab, out var tab))
			_currentTab = tab;
		UpdateTab();
		UpdateFolder();
	}

	private void SaveSettings() {
		if (Config.Modified) Config.SaveConfig();
		Config.WindowWidth = (int)Width;
		Config.WindowHeight = (int)Height;
		Config.CurrentTab = _currentTab.ToString();
		Config.SaveConfig();
	}

	private static void EnsureSteamTerrariaEntry() {
		string path = Config.TerrariaExePath;
		if (string.IsNullOrEmpty(path))
			path = TerrariaLocator.TerrariaPath;
		if (string.IsNullOrEmpty(path)) return;
		if (FolderContainsExe(Config.Games, path)) return;

		string details = TryReadTerrariaVersion(path) ?? "";
		Config.Games.Entries.Insert(0, new Game {
			Name    = "Terraria",
			ExePath = path,
			Icon    = "Tree",
			Details = details,
		});
		Config.Modified = true;
		Config.SaveConfig();
	}

	private static bool FolderContainsExe(SetupFolder folder, string path) {
		foreach (var e in folder.Entries) {
			if (e is Game g && string.Equals(g.ExePath, path, StringComparison.OrdinalIgnoreCase)) return true;
			if (e is SetupFolder sub && FolderContainsExe(sub, path)) return true;
		}
		return false;
	}

	private static string? TryReadTerrariaVersion(string terrariaPath) {
		try {
			string plist = Path.Combine(terrariaPath, "Contents", "Info.plist");
			if (File.Exists(plist)) {
				string text = File.ReadAllText(plist);
				var m = Regex.Match(text, @"<key>CFBundleShortVersionString</key>\s*<string>([^<]+)</string>");
				if (m.Success) return m.Groups[1].Value;
				m = Regex.Match(text, @"<key>CFBundleVersion</key>\s*<string>([^<]+)</string>");
				if (m.Success) return m.Groups[1].Value;
			}
		}
		catch { }
		return null;
	}

	private void LoadSetups() {
		AddTabList(gridGames, _gameStack, Config.Games);
		AddTabList(gridServers, _serverStack, Config.Servers);
		AddTabList(gridTools, _toolStack, Config.Tools);
	}

	private void AddTabList(Grid grid, Stack<TerrariaSetupList> stack, SetupFolder folder) {
		var list = new TerrariaSetupList();
		list.PopulateList(folder, sub => NavigateForward(grid, stack, sub));
		stack.Push(list);
		grid.Children.Add(list);
	}

	public void ReloadSetups() {
		ClearTab(gridGames, _gameStack);
		ClearTab(gridServers, _serverStack);
		ClearTab(gridTools, _toolStack);
		LoadSetups();
	}

	private static void ClearTab(Grid grid, Stack<TerrariaSetupList> stack) {
		grid.Children.Clear();
		stack.Clear();
	}

	private void NavigateForward(Grid grid, Stack<TerrariaSetupList> stack, SetupFolder folder) {
		var list = new TerrariaSetupList();
		list.PopulateList(folder, sub => NavigateForward(grid, stack, sub), () => NavigateBack(grid, stack));
		var last = stack.Peek();
		stack.Push(list);
		grid.Children.Add(list);
		if (!Config.DisableTransitions) {
			last.LeaveFolder(false, grid.Bounds.Width);
			list.EnterFolder(false, grid.Bounds.Width);
		}
		else {
			last.IsVisible = false;
		}
		UpdateFolder();
	}

	private void NavigateBack(Grid grid, Stack<TerrariaSetupList> stack) {
		var last = stack.Pop();
		var prev = stack.Peek();
		if (!Config.DisableTransitions) {
			last.LeaveFolder(true, grid.Bounds.Width);
			prev.EnterFolder(true, grid.Bounds.Width);
		}
		else {
			grid.Children.Remove(last);
			prev.IsVisible = true;
		}
		UpdateFolder();
	}

	private void OnGamesTab(object? sender, RoutedEventArgs e) {
		if (_currentTab == SetupTypes.Game) return;
		if (!Config.DisableTransitions) {
			double w = CurrentGrid.Bounds.Width;
			CurrentStack.Peek().LeaveTab(true, w, false, UpdateTab);
			_gameStack.Peek().EnterTab(true, w, false);
			gridGames.IsVisible = true;
		}
		_currentTab = SetupTypes.Game;
		UpdateFolder();
		if (Config.DisableTransitions) UpdateTab();
	}

	private void OnServersTab(object? sender, RoutedEventArgs e) {
		if (_currentTab == SetupTypes.Server) return;
		if (!Config.DisableTransitions) {
			double w = CurrentGrid.Bounds.Width;
			bool back = _currentTab == SetupTypes.Tool;
			CurrentStack.Peek().LeaveTab(back, w, false, UpdateTab);
			_serverStack.Peek().EnterTab(back, w, false);
			gridServers.IsVisible = true;
		}
		_currentTab = SetupTypes.Server;
		UpdateFolder();
		if (Config.DisableTransitions) UpdateTab();
	}

	private void OnToolsTab(object? sender, RoutedEventArgs e) {
		if (_currentTab == SetupTypes.Tool) return;
		if (!Config.DisableTransitions) {
			double w = CurrentGrid.Bounds.Width;
			CurrentStack.Peek().LeaveTab(false, w, false, UpdateTab);
			_toolStack.Peek().EnterTab(false, w, false);
			gridTools.IsVisible = true;
		}
		_currentTab = SetupTypes.Tool;
		UpdateFolder();
		if (Config.DisableTransitions) UpdateTab();
	}

	private void UpdateTab() {
		gridGames.IsVisible = _currentTab == SetupTypes.Game;
		gridServers.IsVisible = _currentTab == SetupTypes.Server;
		gridTools.IsVisible = _currentTab == SetupTypes.Tool;
	}

	private void UpdateFolder() {
		string label = _currentTab + " List";
		if (CurrentStack.Count > 1)
			label += " " + new string('>', CurrentStack.Count - 1) + " " + CurrentStack.Peek().Folder?.Name;
		labelListType.Text = label;
	}

	private void OnOpenInstancesFolder(object? sender, RoutedEventArgs e) {
		Sounds.PlayOpen();
		try {
			string path = InstanceManager.InstancesRoot;
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
		}
		catch { }
	}

	private async void OnAddInstance(object? sender, RoutedEventArgs e) {
		await AddInstanceWindow.ShowDialogAsync(this);
		ReloadSetups();
	}

	private async void OnEditSetups(object? sender, RoutedEventArgs e) {
		if (await SettingsWindow.ShowDialogAsync(this, _currentTab))
			ReloadSetups();
	}

	private void OnKeyDown(object? sender, KeyEventArgs e) {
		if (e.Key == Key.Back) {
			if (CurrentStack.Count > 1)
				NavigateBack(CurrentGrid, CurrentStack);
		}
	}

	private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e) {
		if (_closing) return;
		_closing = true;
		e.Cancel = true;
		SaveSettings();
		FadeAndClose();
	}

	private async void FadeAndClose() {
		await FadeAsync(1, 0, 0.5);
		Closing -= OnWindowClosing;
		Close();
	}

	private async Task FadeAsync(double from, double to, double seconds) {
		Opacity = from;
		using var cts = new CancellationTokenSource();
		var anim = new Animation {
			Duration = TimeSpan.FromSeconds(seconds),
			FillMode = FillMode.Forward,
			Children = {
				new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(OpacityProperty, from) } },
				new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(OpacityProperty, to) } }
			}
		};
		await anim.RunAsync(this, cts.Token);
		Opacity = to;
	}
}
