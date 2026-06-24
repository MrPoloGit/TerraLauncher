using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using TerraLauncher.Setups;

namespace TerraLauncher.Windows;

public partial class EditServerWindow : Window {
	private string _nonDefaultWorldFolder = "";

	public EditServerWindow(Server server) {
		InitializeComponent();
		EditWindowHelper.InitIconCombo(comboBoxIcon, server.Icon);
		textBoxName.Text = server.Name;
		textBoxDetails.Text = server.Details;
		textBoxExe.Text = server.ExePath;
		textBoxArguments.Text = server.Arguments;
		textBoxWorldFolder.Text = server.WorldDirectory;
		checkBoxDefaultWorldFolder.IsChecked = server.WorldDirectory == "Default";
		textBoxWorldFolder.IsEnabled = server.WorldDirectory != "Default";
		checkBoxTMod.IsChecked = server.IsTMod;
		EditWindowHelper.SetCustomIconState(server.Icon, comboBoxIcon, textBoxCustomIcon, buttonBrowseCustomIcon, checkBoxCustomIcon);
		UpdateIcon(server.Icon);
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

	private void OnDefaultWorldFolderChecked(object? sender, RoutedEventArgs e) {
		if (checkBoxDefaultWorldFolder.IsChecked == true) {
			textBoxWorldFolder.IsEnabled = false;
			_nonDefaultWorldFolder = textBoxWorldFolder.Text ?? "";
			textBoxWorldFolder.Text = "Default";
		}
		else {
			textBoxWorldFolder.IsEnabled = true;
			textBoxWorldFolder.Text = _nonDefaultWorldFolder;
		}
	}

	private async void OnBrowseExe(object? sender, RoutedEventArgs e) {
		var path = await EditWindowHelper.BrowseExe(this, textBoxExe.Text ?? "");
		if (path != null) textBoxExe.Text = path;
	}

	private async void OnBrowseWorldFolder(object? sender, RoutedEventArgs e) {
		var path = await EditWindowHelper.BrowseFolder(this, textBoxWorldFolder.Text ?? "");
		if (path != null) textBoxWorldFolder.Text = path;
	}

	private async void OnBrowseCustomIcon(object? sender, RoutedEventArgs e) {
		var path = await EditWindowHelper.BrowseIcon(this, textBoxCustomIcon.Text ?? "");
		if (path != null) { textBoxCustomIcon.Text = path; UpdateIcon(path); }
	}

	private void UpdateIcon(string icon) {
		var bmp = Setup.LoadIconBitmap(icon, "Server");
		if (bmp != null) {
			imageIcon.Source = bmp;
			imageIcon.Width = Math.Min(68, bmp.PixelSize.Width);
			imageIcon.Height = Math.Min(68, bmp.PixelSize.Height);
		}
	}

	public static async Task<bool> ShowDialogAsync(Window owner, Server server) {
		var w = new EditServerWindow(server);
		var ok = await w.ShowDialog<bool>(owner);
		if (ok) {
			server.Name = w.textBoxName.Text ?? "";
			server.Details = w.textBoxDetails.Text ?? "";
			server.ExePath = w.textBoxExe.Text ?? "";
			server.Arguments = w.textBoxArguments.Text ?? "";
			server.WorldDirectory = w.checkBoxDefaultWorldFolder.IsChecked == true ? "Default" : (w.textBoxWorldFolder.Text ?? "");
			server.Icon = w.checkBoxCustomIcon.IsChecked == true ? (w.textBoxCustomIcon.Text ?? "") : (w.comboBoxIcon.SelectedItem as string ?? "");
			server.IsTMod = w.checkBoxTMod.IsChecked == true;
		}
		return ok;
	}
}
