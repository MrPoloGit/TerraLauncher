using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using TerraLauncher.Setups;

namespace TerraLauncher.Windows;

public partial class EditFolderWindow : Window {
	public EditFolderWindow(SetupFolder folder) {
		InitializeComponent();
		EditWindowHelper.InitIconCombo(comboBoxIcon, folder.Icon);
		textBoxName.Text = folder.Name;
		EditWindowHelper.SetCustomIconState(folder.Icon, comboBoxIcon, textBoxCustomIcon, buttonBrowseCustomIcon, checkBoxCustomIcon);
		UpdateIcon(folder.Icon);
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

	private async void OnBrowseCustomIcon(object? sender, RoutedEventArgs e) {
		var path = await EditWindowHelper.BrowseIcon(this, textBoxCustomIcon.Text ?? "");
		if (path != null) { textBoxCustomIcon.Text = path; UpdateIcon(path); }
	}

	private void UpdateIcon(string icon) {
		var bmp = Setup.LoadFolderIcon(icon);
		if (bmp != null) {
			imageIcon.Source = bmp;
			imageIcon.Width = Math.Min(68, bmp.PixelSize.Width);
			imageIcon.Height = Math.Min(68, bmp.PixelSize.Height);
		}
	}

	public static async Task<bool> ShowDialogAsync(Window owner, SetupFolder folder) {
		var w = new EditFolderWindow(folder);
		var ok = await w.ShowDialog<bool>(owner);
		if (ok) {
			folder.Name = w.textBoxName.Text ?? "";
			folder.Icon = w.checkBoxCustomIcon.IsChecked == true ? (w.textBoxCustomIcon.Text ?? "") : (w.comboBoxIcon.SelectedItem as string ?? "");
		}
		return ok;
	}
}
