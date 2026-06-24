using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using TerraLauncher.Setups;

namespace TerraLauncher.Windows;

internal static class EditWindowHelper {
	public static void InitIconCombo(ComboBox combo, string currentIcon) {
		int index = 0;
		foreach (var pair in Setup.SetupIcons) {
			combo.Items.Add(pair.Key);
			if (currentIcon == pair.Key) combo.SelectedIndex = index;
			index++;
		}
		if (combo.SelectedIndex == -1) combo.SelectedIndex = 0;
	}

	public static void SetCustomIconState(string icon, ComboBox combo, TextBox customBox,
		Button browseBtn, CheckBox customCheck) {
		bool isCustom = !Setup.SetupIcons.ContainsKey(icon);
		customCheck.IsChecked = isCustom;
		combo.IsVisible = !isCustom;
		customBox.IsVisible = isCustom;
		browseBtn.IsVisible = isCustom;
		if (isCustom) customBox.Text = icon;
	}

	public static void ToggleCustomIcon(bool isCustom, ComboBox combo, TextBox customBox,
		Button browseBtn, Action onBuiltIn, Action onCustom) {
		combo.IsVisible = !isCustom;
		customBox.IsVisible = isCustom;
		browseBtn.IsVisible = isCustom;
		if (isCustom) onCustom();
		else onBuiltIn();
	}

	public static async Task<string?> BrowseExe(Window owner, string current) {
		string? dir = null;
		try { dir = Path.GetDirectoryName(current); } catch { }
		var opts = new FilePickerOpenOptions {
			Title = "Choose an Executable",
			AllowMultiple = false,
			FileTypeFilter = new[] {
				new FilePickerFileType("Executable Files") { Patterns = new[] { "*.exe", "*" } },
				FilePickerFileTypes.All
			},
			SuggestedStartLocation = dir != null ? await owner.StorageProvider.TryGetFolderFromPathAsync(dir) : null
		};
		var files = await owner.StorageProvider.OpenFilePickerAsync(opts);
		return files.Count > 0 ? files[0].Path.LocalPath : null;
	}

	public static async Task<string?> BrowseFolder(Window owner, string current) {
		var opts = new FolderPickerOpenOptions {
			Title = "Choose a Folder",
			AllowMultiple = false,
			SuggestedStartLocation = Directory.Exists(current)
				? await owner.StorageProvider.TryGetFolderFromPathAsync(current) : null
		};
		var folders = await owner.StorageProvider.OpenFolderPickerAsync(opts);
		return folders.Count > 0 ? folders[0].Path.LocalPath : null;
	}

	public static async Task<string?> BrowseIcon(Window owner, string current) {
		string? dir = null;
		try { dir = Path.GetDirectoryName(current); } catch { }
		var opts = new FilePickerOpenOptions {
			Title = "Choose an Icon",
			AllowMultiple = false,
			FileTypeFilter = new[] {
				new FilePickerFileType("Image Files") { Patterns = new[] { "*.png", "*.bmp", "*.jpg", "*.ico" } },
				FilePickerFileTypes.All
			},
			SuggestedStartLocation = dir != null ? await owner.StorageProvider.TryGetFolderFromPathAsync(dir) : null
		};
		var files = await owner.StorageProvider.OpenFilePickerAsync(opts);
		return files.Count > 0 ? files[0].Path.LocalPath : null;
	}

	public static async Task<string?> BrowseProject(Window owner, string current) {
		string? dir = null;
		try { dir = Path.GetDirectoryName(current); } catch { }
		var opts = new FilePickerOpenOptions {
			Title = "Choose a Project File",
			AllowMultiple = false,
			FileTypeFilter = new[] {
				new FilePickerFileType("Solution Files") { Patterns = new[] { "*.sln" } },
				FilePickerFileTypes.All
			},
			SuggestedStartLocation = dir != null ? await owner.StorageProvider.TryGetFolderFromPathAsync(dir) : null
		};
		var files = await owner.StorageProvider.OpenFilePickerAsync(opts);
		return files.Count > 0 ? files[0].Path.LocalPath : null;
	}
}
