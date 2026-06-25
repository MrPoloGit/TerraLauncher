using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using TerraLauncher.Controls.Terraria;
using TerraLauncher.Instances;

namespace TerraLauncher.Windows;

public partial class VersionPickerWindow : Window {
	private readonly InstanceCategory _category;
	private bool _closing;

	public VersionPickerWindow(InstanceCategory category) {
		InitializeComponent();
		_category = category;
		labelCategory.Text = CategoryLabel(category);

		Opened += async (_, _) => {
			Sounds.PlayOpen();
			await FadeAsync(0, 1, 0.3);
			await LoadVersionsAsync();
		};
		Closing += OnWindowClosing;
	}

	private static string CategoryLabel(InstanceCategory c) => c switch {
		InstanceCategory.Terraria   => "Terraria Versions",
		InstanceCategory.TModLoader => "tModLoader",
		InstanceCategory.TAPI       => "tAPI",
		InstanceCategory.TConfig    => "tConfig",
		InstanceCategory.StandAlone => "StandAlone",
		_                           => c.ToString()
	};

	// ── Version loading ────────────────────────────────────────────────

	private async Task LoadVersionsAsync() {
		labelStatus.Text = "Loading…";
		versionList.Children.Clear();

		List<VersionEntry> versions;
		try {
			versions = await VersionSource.GetVersionsAsync(_category);
		}
		catch (Exception ex) {
			labelStatus.Text = "Failed to load: " + ex.Message;
			return;
		}

		if (versions.Count == 0) {
			labelStatus.Text = "No versions found.";
			return;
		}

		foreach (var entry in versions)
			versionList.Children.Add(BuildRow(entry));

		labelStatus.Text = $"{versions.Count} version(s)";
	}

	private Control BuildRow(VersionEntry entry) {
		var border = new Border {
			Background      = new SolidColorBrush(Color.Parse("#1E1E5A")),
			BorderBrush     = new SolidColorBrush(Color.Parse("#14143A")),
			BorderThickness = new Thickness(1),
			CornerRadius    = new CornerRadius(4),
			Padding         = new Thickness(10, 8),
			Margin          = new Thickness(0, 0, 0, 4),
		};

		var grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
		grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

		var info = new StackPanel { Spacing = 3 };
		info.Children.Add(new TextBlock {
			Text       = entry.Name,
			FontSize   = 18,
			Foreground = Brushes.White,
		});

		if (!string.IsNullOrEmpty(entry.Description)) {
			info.Children.Add(new TextBlock {
				Text         = entry.Description,
				FontSize     = 12,
				Foreground   = new SolidColorBrush(Color.Parse("#AAAACC")),
				TextWrapping = TextWrapping.Wrap,
			});
		}

		var meta = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
		if (!string.IsNullOrEmpty(entry.Date))
			meta.Children.Add(new TextBlock { Text = entry.Date, FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#888899")), VerticalAlignment = VerticalAlignment.Center });
		if (!string.IsNullOrEmpty(entry.Author))
			meta.Children.Add(new TextBlock { Text = "by " + entry.Author, FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#888899")), VerticalAlignment = VerticalAlignment.Center });
		if (!string.IsNullOrEmpty(entry.RequiresTerrariaVersion))
			meta.Children.Add(new TextBlock { Text = "⚠ Requires Terraria " + entry.RequiresTerrariaVersion, FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#FFAA44")), VerticalAlignment = VerticalAlignment.Center, LineHeight = 16 });
		info.Children.Add(meta);

		Grid.SetColumn(info, 0);
		grid.Children.Add(info);

		var btn = new TerrariaButton { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };
		btn.Content = new TextBlock { Text = "Download", Foreground = Brushes.White, FontSize = 16, Margin = new Thickness(10, 4) };
		btn.Click += async (_, _) => await OnDownload(entry);
		Grid.SetColumn(btn, 1);
		grid.Children.Add(btn);

		border.Child = grid;
		return border;
	}

	private async Task OnDownload(VersionEntry entry) {
		if (!string.IsNullOrEmpty(entry.RequiresTerrariaVersion)) {
			var existing = InstanceManager.FindTerrariaVersion(entry.RequiresTerrariaVersion);
			if (existing == null) {
				var result = await TriggerMessageBox.ShowAsync(
					this,
					MessageIcon.Question,
					$"{entry.Name} requires Terraria {entry.RequiresTerrariaVersion} which is not installed.\n\nDownload it automatically?",
					"Dependency Required",
					MsgBoxButton.YesNo);
				if (result != MsgBoxResult.Yes) return;

				await StubDownloadAsync(new VersionEntry {
					Name    = $"Terraria {entry.RequiresTerrariaVersion}",
					Version = entry.RequiresTerrariaVersion,
				}, InstanceCategory.Terraria);
			}
		}

		await StubDownloadAsync(entry, _category);

		_closing = true;
		Closing -= OnWindowClosing;
		Sounds.PlayClose();
		await FadeAsync(1, 0, 0.25);
		Close(true);
	}

	private static Task StubDownloadAsync(VersionEntry entry, InstanceCategory category) {
		// Stub: creates directory + marker file. Real download implemented in steps 6–9.
		string safeName = (category.ToString() + "-" + entry.Version)
			.Replace(" ", "-").Replace("/", "-");
		string installDir = InstanceManager.GetInstallDir(category, safeName);
		Directory.CreateDirectory(installDir);

		string stubExe = Path.Combine(installDir,
			OperatingSystem.IsWindows() ? "stub.exe" : "stub");
		File.WriteAllText(stubExe, $"Stub for {entry.Name} — real download not yet implemented.");

		InstanceManager.AddInstance(new InstanceRecord {
			Name        = entry.Name,
			Version     = entry.Version,
			Category    = category,
			InstallPath = installDir,
			ExePath     = stubExe,
		});
		return Task.CompletedTask;
	}

	private async void OnRefresh(object? sender, RoutedEventArgs e) => await LoadVersionsAsync();

	// ── Window chrome ──────────────────────────────────────────────────

	private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e) {
		if (_closing) return;
		_closing = true;
		e.Cancel = true;
		FadeAndClose();
	}

	private async void FadeAndClose() {
		await FadeAsync(1, 0, 0.3);
		Closing -= OnWindowClosing;
		Close(false);
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
}
