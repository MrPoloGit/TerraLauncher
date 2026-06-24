using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using TerraLauncher.Setups;

namespace TerraLauncher.Windows;

public partial class EditToolWindow : Window {
	public EditToolWindow(Tool tool) {
		InitializeComponent();
		EditWindowHelper.InitIconCombo(comboBoxIcon, tool.Icon);
		textBoxName.Text = tool.Name;
		textBoxDetails.Text = tool.Details;
		textBoxExe.Text = tool.ExePath;
		textBoxArguments.Text = tool.Arguments;
		textBoxProject.Text = tool.ProjectPath;
		checkBoxDeveloper.IsChecked = !string.IsNullOrWhiteSpace(tool.ProjectPath);
		textBoxProject.IsEnabled = !string.IsNullOrWhiteSpace(tool.ProjectPath);
		EditWindowHelper.SetCustomIconState(tool.Icon, comboBoxIcon, textBoxCustomIcon, buttonBrowseCustomIcon, checkBoxCustomIcon);
		UpdateIcon(tool.Icon);
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

	private void OnDeveloperChecked(object? sender, RoutedEventArgs e) {
		textBoxProject.IsEnabled = checkBoxDeveloper.IsChecked == true;
	}

	private async void OnBrowseExe(object? sender, RoutedEventArgs e) {
		var path = await EditWindowHelper.BrowseExe(this, textBoxExe.Text ?? "");
		if (path != null) textBoxExe.Text = path;
	}

	private async void OnBrowseProject(object? sender, RoutedEventArgs e) {
		var path = await EditWindowHelper.BrowseProject(this, textBoxProject.Text ?? "");
		if (path != null) textBoxProject.Text = path;
	}

	private async void OnBrowseCustomIcon(object? sender, RoutedEventArgs e) {
		var path = await EditWindowHelper.BrowseIcon(this, textBoxCustomIcon.Text ?? "");
		if (path != null) { textBoxCustomIcon.Text = path; UpdateIcon(path); }
	}

	private void UpdateIcon(string icon) {
		var bmp = Setup.LoadIconBitmap(icon, "Tool");
		if (bmp != null) {
			imageIcon.Source = bmp;
			imageIcon.Width = Math.Min(68, bmp.PixelSize.Width);
			imageIcon.Height = Math.Min(68, bmp.PixelSize.Height);
		}
	}

	public static async Task<bool> ShowDialogAsync(Window owner, Tool tool) {
		var w = new EditToolWindow(tool);
		var ok = await w.ShowDialog<bool>(owner);
		if (ok) {
			tool.Name = w.textBoxName.Text ?? "";
			tool.Details = w.textBoxDetails.Text ?? "";
			tool.ExePath = w.textBoxExe.Text ?? "";
			tool.Arguments = w.textBoxArguments.Text ?? "";
			tool.ProjectPath = w.checkBoxDeveloper.IsChecked == true ? (w.textBoxProject.Text ?? "") : "";
			tool.Icon = w.checkBoxCustomIcon.IsChecked == true ? (w.textBoxCustomIcon.Text ?? "") : (w.comboBoxIcon.SelectedItem as string ?? "");
		}
		return ok;
	}
}
