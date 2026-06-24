using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TerraLauncher.Setups;

namespace TerraLauncher.Windows;

public partial class EditGameWindow : Window {
	private string _nonDefaultSaveFolder = "";

	public EditGameWindow(Game game) {
		InitializeComponent();
		EditWindowHelper.InitIconCombo(comboBoxIcon, game.Icon);
		textBoxName.Text = game.Name;
		textBoxDetails.Text = game.Details;
		textBoxExe.Text = game.ExePath;
		textBoxSaveFolder.Text = game.SaveDirectory;
		checkBoxDefaultSaveFolder.IsChecked = game.SaveDirectory == "Default";
		textBoxSaveFolder.IsEnabled = game.SaveDirectory != "Default";
		checkBoxTMod.IsChecked = game.IsTMod;
		EditWindowHelper.SetCustomIconState(game.Icon, comboBoxIcon, textBoxCustomIcon, buttonBrowseCustomIcon, checkBoxCustomIcon);
		UpdateIcon(game.Icon);
	}

	private void OnOKClicked(object? sender, RoutedEventArgs e) => Close(true);

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
			_nonDefaultSaveFolder = textBoxSaveFolder.Text ?? "";
			textBoxSaveFolder.Text = "Default";
		}
		else {
			textBoxSaveFolder.IsEnabled = true;
			textBoxSaveFolder.Text = _nonDefaultSaveFolder;
		}
	}

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
			imageIcon.Width = Math.Min(68, bmp.PixelSize.Width);
			imageIcon.Height = Math.Min(68, bmp.PixelSize.Height);
		}
	}

	public static async Task<bool> ShowDialogAsync(Window owner, Game game) {
		var w = new EditGameWindow(game);
		var ok = await w.ShowDialog<bool>(owner);
		if (ok) {
			game.Name = w.textBoxName.Text ?? "";
			game.Details = w.textBoxDetails.Text ?? "";
			game.ExePath = w.textBoxExe.Text ?? "";
			game.SaveDirectory = w.checkBoxDefaultSaveFolder.IsChecked == true ? "Default" : (w.textBoxSaveFolder.Text ?? "");
			game.Icon = w.checkBoxCustomIcon.IsChecked == true ? (w.textBoxCustomIcon.Text ?? "") : (w.comboBoxIcon.SelectedItem as string ?? "");
			game.IsTMod = w.checkBoxTMod.IsChecked == true;
		}
		return ok;
	}
}
