using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using TerraLauncher.Setups;

namespace TerraLauncher.Windows;

public partial class SettingsWindow : Window {
	private bool _closing;

	public SettingsWindow(SetupTypes startupTab = SetupTypes.Game) {
		InitializeComponent();

		if (Config.SettingsWidth >= MinWidth)   Width  = Config.SettingsWidth;
		if (Config.SettingsHeight >= MinHeight) Height = Config.SettingsHeight;

		checkBoxCloseGame.IsChecked          = Config.CloseOnGameLaunch;
		checkBoxDisableTransitions.IsChecked = Config.DisableTransitions;
		checkBoxMuted.IsChecked              = Config.Muted;
		checkBoxIntegration.IsChecked        = Config.Integration;
		spinnerScrollSpeed.Value             = (int)(Config.ScrollSpeed * 100);
		textBoxTerrariaPath.Text             = Config.TerrariaExePath;

		Opened  += async (_, _) => { Sounds.PlayOpen(); await FadeAsync(0, 1, 0.3); };
		Closing += OnWindowClosing;
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

	private async void OnBrowseTerrariaPath(object? sender, RoutedEventArgs e) {
		var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
			Title = "Select Terraria Executable",
			AllowMultiple = false,
		});
		if (files.Count > 0)
			textBoxTerrariaPath.Text = files[0].TryGetLocalPath() ?? "";
	}

	// ── Help links ─────────────────────────────────────────────────────

	private void OnAbout(object? sender, RoutedEventArgs e)        => AboutWindow.Show(this);
	private void OnCredits(object? sender, RoutedEventArgs e)      => CreditsWindow.Show(this);
	private void OnViewOnGitHub(object? sender, RoutedEventArgs e) =>
		Process.Start(new ProcessStartInfo("https://github.com/MrPoloGit/TerraLauncher") { UseShellExecute = true });

	// ── ShowDialog helper ──────────────────────────────────────────────

	public static async Task<bool> ShowDialogAsync(Window owner, SetupTypes startupTab = SetupTypes.Game) {
		var w = new SettingsWindow(startupTab);
		var ok = await w.ShowDialog<bool>(owner);

		Config.SettingsWidth  = (int)w.Width;
		Config.SettingsHeight = (int)w.Height;

		if (ok) {
			Config.TerrariaExePath     = w.textBoxTerrariaPath.Text?.Trim()       ?? "";
			Config.CloseOnGameLaunch   = w.checkBoxCloseGame.IsChecked          == true;
			Config.DisableTransitions  = w.checkBoxDisableTransitions.IsChecked == true;
			Config.Muted               = w.checkBoxMuted.IsChecked              == true;
			Config.Integration         = w.checkBoxIntegration.IsChecked        == true;
			Config.ScrollSpeed         = w.spinnerScrollSpeed.Value / 100.0;
			Config.SaveConfig();
		}
		return ok;
	}
}
