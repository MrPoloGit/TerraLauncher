using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using TerraLauncher.Setups;

namespace TerraLauncher.Windows;

public partial class EditGameWindow : Window {
	private string _nonDefaultSaveFolder = "";
	private bool _closing;

	public EditGameWindow(Game game) {
		InitializeComponent();
		EditWindowHelper.InitIconCombo(comboBoxIcon, game.Icon);
		textBoxName.Text       = game.Name;
		textBoxDetails.Text    = game.Details;
		textBoxExe.Text        = game.ExePath;
		textBoxSaveFolder.Text = game.SaveDirectory;
		checkBoxDefaultSaveFolder.IsChecked = game.SaveDirectory == "Default";
		textBoxSaveFolder.IsEnabled         = game.SaveDirectory != "Default";
		checkBoxTMod.IsChecked              = game.IsTMod;
		EditWindowHelper.SetCustomIconState(game.Icon, comboBoxIcon, textBoxCustomIcon, buttonBrowseCustomIcon, checkBoxCustomIcon);
		UpdateIcon(game.Icon);

		Opened  += async (_, _) => { Sounds.PlayOpen(); await FadeAsync(0, 1, 0.25); };
		Closing += OnWindowClosing;
	}

	// ── Button handlers ────────────────────────────────────────────────

	private void OnOKClicked(object? sender, RoutedEventArgs e) {
		if (_closing) return;
		FadeAndClose(true);
	}

	private void OnCancelClicked(object? sender, RoutedEventArgs e) {
		if (_closing) return;
		FadeAndClose(false);
	}

	private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e) {
		if (_closing) return;
		_closing = true;
		e.Cancel = true;
		FadeAndClose(false);
	}

	private async void FadeAndClose(bool result) {
		_closing = true;
		Closing -= OnWindowClosing;
		Sounds.PlayClose();
		await FadeAsync(1, 0, 0.2);
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

	// ── Icon selection ─────────────────────────────────────────────────

	private void OnIconSelectionChanged(object? sender, SelectionChangedEventArgs e) {
		if (comboBoxIcon.SelectedItem is string icon) UpdateIcon(icon);
	}

	private void OnCustomIconLostFocus(object? sender, RoutedEventArgs e) {
		if (checkBoxCustomIcon.IsChecked == true) UpdateIcon(textBoxCustomIcon.Text ?? "");
	}

	private void OnCustomIconChecked(object? sender, RoutedEventArgs e) {
		EditWindowHelper.ToggleCustomIcon(checkBoxCustomIcon.IsChecked == true,
			comboBoxIcon, textBoxCustomIcon, buttonBrowseCustomIcon,
			() => UpdateIcon(comboBoxIcon.SelectedItem as string ?? ""),
			() => UpdateIcon(textBoxCustomIcon.Text ?? ""));
	}

	private void OnDefaultSaveFolderChecked(object? sender, RoutedEventArgs e) {
		if (checkBoxDefaultSaveFolder.IsChecked == true) {
			textBoxSaveFolder.IsEnabled = false;
			_nonDefaultSaveFolder       = textBoxSaveFolder.Text ?? "";
			textBoxSaveFolder.Text      = "Default";
		}
		else {
			textBoxSaveFolder.IsEnabled = true;
			textBoxSaveFolder.Text      = _nonDefaultSaveFolder;
		}
	}

	// ── Browse helpers ─────────────────────────────────────────────────

	private async void OnBrowseExe(object? sender, RoutedEventArgs e) {
		var path = await EditWindowHelper.BrowseExe(this, textBoxExe.Text ?? "");
		if (path != null) textBoxExe.Text = path;
	}

	private async void OnBrowseSaveFolder(object? sender, RoutedEventArgs e) {
		var path = await EditWindowHelper.BrowseFolder(this, textBoxSaveFolder.Text ?? "");
		if (path != null) textBoxSaveFolder.Text = path;
	}

	private async void OnBrowseCustomIcon(object? sender, RoutedEventArgs e) {
		var path = await EditWindowHelper.BrowseIcon(this, textBoxCustomIcon.Text ?? "");
		if (path != null) { textBoxCustomIcon.Text = path; UpdateIcon(path); }
	}

	private void UpdateIcon(string icon) {
		var bmp = Setup.LoadIconBitmap(icon, "Tree");
		if (bmp != null) {
			imageIcon.Source = bmp;
			imageIcon.Width  = Math.Min(68, bmp.PixelSize.Width);
			imageIcon.Height = Math.Min(68, bmp.PixelSize.Height);
		}
	}

	// ── ShowDialog helper ──────────────────────────────────────────────

	public static async Task<bool> ShowDialogAsync(Window owner, Game game) {
		var w  = new EditGameWindow(game);
		var ok = await w.ShowDialog<bool>(owner);
		if (ok) {
			game.Name          = w.textBoxName.Text ?? "";
			game.Details       = w.textBoxDetails.Text ?? "";
			game.ExePath       = w.textBoxExe.Text ?? "";
			game.SaveDirectory = w.checkBoxDefaultSaveFolder.IsChecked == true
				? "Default" : (w.textBoxSaveFolder.Text ?? "");
			game.Icon          = w.checkBoxCustomIcon.IsChecked == true
				? (w.textBoxCustomIcon.Text ?? "") : (w.comboBoxIcon.SelectedItem as string ?? "");
			game.IsTMod        = w.checkBoxTMod.IsChecked == true;
		}
		return ok;
	}
}
