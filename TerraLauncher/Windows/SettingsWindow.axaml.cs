using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using TerraLauncher.Setups;

namespace TerraLauncher.Windows;

public partial class SettingsWindow : Window {
	private bool _closing;
	private SetupTypes _currentTab;

	public SettingsWindow(SetupTypes startupTab) {
		InitializeComponent();

		_currentTab = startupTab;

		treeViewGames.Populate(Config.Games, SetupTypes.Game);
		treeViewServers.Populate(Config.Servers, SetupTypes.Server);
		treeViewTools.Populate(Config.Tools, SetupTypes.Tool);

		if (Config.SettingsWidth >= MinWidth)   Width  = Config.SettingsWidth;
		if (Config.SettingsHeight >= MinHeight) Height = Config.SettingsHeight;

		checkBoxCloseGame.IsChecked          = Config.CloseOnGameLaunch;
		checkBoxCloseServer.IsChecked        = Config.CloseOnServerLaunch;
		checkBoxCloseTool.IsChecked          = Config.CloseOnToolLaunch;
		checkBoxDisableTransitions.IsChecked = Config.DisableTransitions;
		checkBoxMuted.IsChecked              = Config.Muted;
		checkBoxIntegration.IsChecked        = Config.Integration;
		spinnerScrollSpeed.Value             = (int)(Config.ScrollSpeed * 100);

		UpdateTab();

		Opened  += async (_, _) => { Sounds.PlayOpen(); await FadeAsync(0, 1, 0.3); };
		Closing += OnWindowClosing;
	}

	// ── Tab switching ──────────────────────────────────────────────────

	private void OnGamesTab(object? sender, RoutedEventArgs e) {
		_currentTab = SetupTypes.Game;
		UpdateTab();
	}
	private void OnServersTab(object? sender, RoutedEventArgs e) {
		_currentTab = SetupTypes.Server;
		UpdateTab();
	}
	private void OnToolsTab(object? sender, RoutedEventArgs e) {
		_currentTab = SetupTypes.Tool;
		UpdateTab();
	}
	private void OnSettingsTab(object? sender, RoutedEventArgs e) {
		_currentTab = (SetupTypes)(-1);
		UpdateTab();
	}

	private void UpdateTab() {
		bool isSettings = (int)_currentTab < 0;
		treeViewGames.IsVisible   = _currentTab == SetupTypes.Game;
		treeViewServers.IsVisible = _currentTab == SetupTypes.Server;
		treeViewTools.IsVisible   = _currentTab == SetupTypes.Tool;
		panelSettings.IsVisible   = isSettings;
		labelTab.Text = isSettings ? "Settings" : _currentTab + " List";
	}

	// ── Close / Save ───────────────────────────────────────────────────

	private void OnSaveClicked(object? sender, RoutedEventArgs e) {
		if (_closing) return;
		_closing = true;
		Closing -= OnWindowClosing;
		Sounds.PlayClose();
		FadeAndClose(true);
	}

	private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e) {
		if (_closing) return;
		_closing = true;
		e.Cancel = true;
		Sounds.PlayClose();
		FadeAndClose(false);
	}

	private async void FadeAndClose(bool result) {
		await FadeAsync(1, 0, 0.3);
		Closing -= OnWindowClosing;
		Close(result);
	}

	private async Task FadeAsync(double from, double to, double seconds) {
		Opacity = from;
		var anim = new Animation {
			Duration = TimeSpan.FromSeconds(seconds),
			FillMode = FillMode.Forward,
			Children = {
				new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(OpacityProperty, from) } },
				new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(OpacityProperty, to) } }
			}
		};
		using var cts = new CancellationTokenSource();
		await anim.RunAsync(this, cts.Token);
		Opacity = to;
	}

	// ── Help links ─────────────────────────────────────────────────────

	private void OnAbout(object? sender, RoutedEventArgs e)        => AboutWindow.Show(this);
	private void OnCredits(object? sender, RoutedEventArgs e)      => CreditsWindow.Show(this);
	private void OnViewOnGitHub(object? sender, RoutedEventArgs e) =>
		Process.Start(new ProcessStartInfo("https://github.com/trigger-death/TerraLauncher") { UseShellExecute = true });

	// ── ShowDialog helper ──────────────────────────────────────────────

	public static async Task<bool> ShowDialogAsync(Window owner, SetupTypes startupTab) {
		var w = new SettingsWindow(startupTab);
		var ok = await w.ShowDialog<bool>(owner);

		Config.SettingsWidth  = (int)w.Width;
		Config.SettingsHeight = (int)w.Height;

		if (ok) {
			if (w.treeViewGames.Modified)   { Config.Games   = w.treeViewGames.GenerateHierarchy();   Config.Modified = true; }
			if (w.treeViewServers.Modified) { Config.Servers = w.treeViewServers.GenerateHierarchy(); Config.Modified = true; }
			if (w.treeViewTools.Modified)   { Config.Tools   = w.treeViewTools.GenerateHierarchy();   Config.Modified = true; }
			Config.CloseOnGameLaunch   = w.checkBoxCloseGame.IsChecked   == true;
			Config.CloseOnServerLaunch = w.checkBoxCloseServer.IsChecked == true;
			Config.CloseOnToolLaunch   = w.checkBoxCloseTool.IsChecked   == true;
			Config.DisableTransitions  = w.checkBoxDisableTransitions.IsChecked == true;
			Config.Muted               = w.checkBoxMuted.IsChecked       == true;
			Config.Integration         = w.checkBoxIntegration.IsChecked == true;
			Config.ScrollSpeed         = w.spinnerScrollSpeed.Value / 100.0;
			Config.SaveConfig();
		}
		return ok;
	}
}
