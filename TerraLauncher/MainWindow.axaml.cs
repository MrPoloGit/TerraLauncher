using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

	private static readonly string[] FilterNames =
		{ "All Instances", "Terraria", "tModLoader", "tAPI", "tConfig", "StandAlone" };
	private static readonly InstanceCategory?[] FilterCategories =
		{ null, InstanceCategory.Terraria, InstanceCategory.TModLoader,
		  InstanceCategory.TAPI, InstanceCategory.TConfig, InstanceCategory.StandAlone };

	public MainWindow() {
		InitializeComponent();
		Config.MainWindow = this;
		comboFilter.ItemsSource = FilterNames;
		comboFilter.SelectedIndex = 0;
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
	}

	private void SaveSettings() {
		if (Config.Modified) Config.SaveConfig();
		Config.WindowWidth = (int)Width;
		Config.WindowHeight = (int)Height;
		Config.SaveConfig();
	}

	private static void EnsureSteamTerrariaEntry() {
		if (Config.HideSteamTerraria) return;
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
		var list = new TerrariaSetupList();
		list.PopulateList(Config.Games, sub => NavigateForward(sub));
		_gameStack.Push(list);
		gridGames.Children.Add(list);
	}

	public void ReloadSetups() {
		gridGames.Children.Clear();
		_gameStack.Clear();
		LoadSetups();
		ApplyFilter();
	}

	private void NavigateForward(SetupFolder folder) {
		var list = new TerrariaSetupList();
		list.PopulateList(folder, NavigateForward, NavigateBack);
		var last = _gameStack.Peek();
		_gameStack.Push(list);
		gridGames.Children.Add(list);
		if (!Config.DisableTransitions) {
			last.LeaveFolder(false, gridGames.Bounds.Width);
			list.EnterFolder(false, gridGames.Bounds.Width);
		}
		else {
			last.IsVisible = false;
		}
	}

	private void NavigateBack() {
		var last = _gameStack.Pop();
		var prev = _gameStack.Peek();
		if (!Config.DisableTransitions) {
			last.LeaveFolder(true, gridGames.Bounds.Width);
			prev.EnterFolder(true, gridGames.Bounds.Width);
		}
		else {
			gridGames.Children.Remove(last);
			prev.IsVisible = true;
		}
	}

	// ── Search + instance filter ───────────────────────────────────────

	private void OnSearchChanged(object? sender, TextChangedEventArgs e) => ApplyFilter();
	private void OnFilterChanged(object? sender, SelectionChangedEventArgs e) => ApplyFilter();

	private void ApplyFilter() {
		if (_gameStack.Count == 0) return;
		var list = _gameStack.Peek();

		string query = textBoxSearch.Text?.Trim() ?? "";
		int idx = Math.Max(0, comboFilter.SelectedIndex);
		InstanceCategory? category = FilterCategories[Math.Min(idx, FilterCategories.Length - 1)];

		if (query.Length == 0 && category == null) {
			list.PopulateList(list.Folder ?? Config.Games, NavigateForward, NavigateBack);
			return;
		}

		// Flatten the whole tree so search/filter reaches into folders
		var matches = new List<Setup>();
		void Walk(SetupFolder folder) {
			foreach (var entry in folder.Entries) {
				if (entry is SetupFolder sub) { Walk(sub); continue; }
				if (entry is not Setup setup) continue;
				if (query.Length > 0
					&& !setup.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
					&& !setup.Details.Contains(query, StringComparison.OrdinalIgnoreCase)) continue;
				if (category != null && CategoryOf(setup) != category) continue;
				matches.Add(setup);
			}
		}
		Walk(Config.Games);

		string what = category != null ? $"{FilterNames[idx]} instances" : "instances";
		string emptyMessage = query.Length > 0
			? $"No {what} match \"{query}\"."
			: $"No {what} installed.";
		list.PopulateFlat(matches, emptyMessage);
	}

	private static InstanceCategory? CategoryOf(Setup setup) {
		var record = InstanceManager.Instances.FirstOrDefault(r => r.ExePath == setup.ExePath);
		if (record != null) return record.Category;
		// Entries with no instance record (e.g. the Steam-detected install)
		if (setup is Game g)
			return g.IsTMod ? InstanceCategory.TModLoader : InstanceCategory.Terraria;
		return null;
	}

	// ── Bottom bar buttons ─────────────────────────────────────────────

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
		if (await SettingsWindow.ShowDialogAsync(this, SetupTypes.Game))
			ReloadSetups();
	}

	private void OnKeyDown(object? sender, KeyEventArgs e) {
		if (e.Key == Key.Back && !textBoxSearch.IsFocused) {
			if (_gameStack.Count > 1)
				NavigateBack();
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
