using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using TerraLauncher.Instances;

namespace TerraLauncher.Windows;

// "Custom" category: link any executable/app/script and choose which
// instance category it files under.
public partial class CustomInstanceWindow : Window {
	private static readonly string[] CategoryNames =
		{ "Custom", "Terraria", "tModLoader", "tAPI", "tConfig", "StandAlone" };
	private static readonly InstanceCategory[] Categories =
		{ InstanceCategory.Custom, InstanceCategory.Terraria, InstanceCategory.TModLoader,
		  InstanceCategory.TAPI, InstanceCategory.TConfig, InstanceCategory.StandAlone };

	private bool _closing;
	private bool _added;

	public CustomInstanceWindow() {
		InitializeComponent();
		comboCategory.ItemsSource = CategoryNames;
		comboCategory.SelectedIndex = 0;
		Opened  += async (_, _) => { Sounds.PlayOpen(); await FadeAsync(0, 1, 0.25); };
		Closing += OnWindowClosing;
	}

	private async void OnBrowse(object? sender, RoutedEventArgs e) {
		var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
			Title = "Select Executable",
			AllowMultiple = false,
		});
		if (files.Count > 0) {
			string? path = files[0].TryGetLocalPath();
			if (path != null) {
				textBoxPath.Text = path;
				if (string.IsNullOrWhiteSpace(textBoxName.Text))
					textBoxName.Text = Path.GetFileNameWithoutExtension(path);
			}
		}
	}

	private async void OnAdd(object? sender, RoutedEventArgs e) {
		string path = textBoxPath.Text?.Trim() ?? "";
		if (path.Length == 0 || (!File.Exists(path) && !Directory.Exists(path))) {
			await TriggerMessageBox.ShowAsync(this, MessageIcon.Error,
				"Select a valid executable, app, or script to launch.", "Invalid Path");
			return;
		}

		string name = textBoxName.Text?.Trim() ?? "";
		if (name.Length == 0)
			name = Path.GetFileNameWithoutExtension(path);

		int idx = Math.Max(0, comboCategory.SelectedIndex);
		InstanceManager.AddInstance(new InstanceRecord {
			Name     = name,
			Version  = "",
			Category = Categories[Math.Min(idx, Categories.Length - 1)],
			ExePath  = path,
			// No InstallPath: nothing on disk belongs to the launcher, so
			// deleting this instance never offers to delete files.
		});

		_added = true;
		FadeAndClose();
	}

	private void OnCancel(object? sender, RoutedEventArgs e) => FadeAndClose();

	private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e) {
		if (_closing) return;
		_closing = true;
		e.Cancel = true;
		FadeAndClose();
	}

	private async void FadeAndClose() {
		_closing = true;
		Closing -= OnWindowClosing;
		Sounds.PlayClose();
		await FadeAsync(1, 0, 0.2);
		Close(_added);
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

	public static async Task<bool> ShowDialogAsync(Window owner) {
		var w = new CustomInstanceWindow();
		return await w.ShowDialog<bool>(owner);
	}
}
