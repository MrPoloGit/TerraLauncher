using System;
using System.Collections.Generic;
using System.Linq;
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
		bool installed = InstanceManager.Instances.Any(
			r => r.Category == _category && r.Version == entry.Version);
		if (installed) {
			btn.Content = new TextBlock {
				Text = "Installed", Foreground = new SolidColorBrush(Color.Parse("#888899")),
				FontSize = 16, Margin = new Thickness(10, 4)
			};
			btn.IsEnabled = false;
			btn.Opacity = 0.55;
			btn.Cursor = Avalonia.Input.Cursor.Default;
		}
		else {
			btn.Content = new TextBlock { Text = "Download", Foreground = Brushes.White, FontSize = 16, Margin = new Thickness(10, 4) };
			btn.Click += async (_, _) => await OnDownload(entry);
		}
		Grid.SetColumn(btn, 1);
		grid.Children.Add(btn);

		border.Child = grid;
		return border;
	}

	private async Task OnDownload(VersionEntry entry) {
		// Resolve the Terraria dependency first (PLAN Feature 5)
		string? linkedTerrariaId = null;
		if (!string.IsNullOrEmpty(entry.RequiresTerrariaVersion)) {
			string required = entry.RequiresTerrariaVersion;
			var existing = InstanceManager.FindTerrariaVersion(required);
			if (existing == null) {
				var result = await TriggerMessageBox.ShowAsync(
					this,
					MessageIcon.Question,
					$"{entry.Name} requires Terraria {required} which is not installed.\n\nDownload it automatically? (Requires Steam login)",
					"Dependency Required",
					MsgBoxButton.YesNo);
				if (result != MsgBoxResult.Yes) return;

				var terrariaVersions = await VersionSource.GetVersionsAsync(InstanceCategory.Terraria);
				var dep = terrariaVersions.FirstOrDefault(v => v.Version == required)
					?? new VersionEntry { Name = $"Terraria {required}", Version = required };
				if (!await DownloadAsync(dep, InstanceCategory.Terraria, null)) return;
				existing = InstanceManager.FindTerrariaVersion(required);
			}
			linkedTerrariaId = existing?.Id;
		}

		if (!await DownloadAsync(entry, _category, linkedTerrariaId)) return;

		_closing = true;
		Closing -= OnWindowClosing;
		Sounds.PlayClose();
		await FadeAsync(1, 0, 0.25);
		Close(true);
	}

	private async Task<bool> DownloadAsync(VersionEntry entry, InstanceCategory category, string? linkedTerrariaId) {
		if (InstanceManager.Instances.Any(r => r.Category == category && r.Version == entry.Version)) {
			await TriggerMessageBox.ShowAsync(this, MessageIcon.Info,
				$"{entry.Name} is already installed.", "Already Installed");
			return false;
		}

		string user = "", pass = "";
		if (category == InstanceCategory.Terraria) {
			var login = await TextPromptWindow.ShowLoginAsync(this, "Steam Login",
				"Steam credentials are required to download Terraria.\n" +
				"They are passed directly to DepotDownloader and never saved.",
				"Steam Username", "Steam Password");
			if (login == null || string.IsNullOrWhiteSpace(login.Value.User)) return false;
			(user, pass) = login.Value;
		}
		else if (string.IsNullOrEmpty(entry.Url) || entry.Url.Contains("TODO")) {
			await TriggerMessageBox.ShowAsync(this, MessageIcon.Error,
				$"No download is available for {entry.Name} yet.", "Not Available");
			return false;
		}

		string safeName = (category.ToString() + "-" + entry.Version)
			.Replace(" ", "-").Replace("/", "-");
		string installDir = InstanceManager.GetInstallDir(category, safeName);

		string? exePath = null;
		bool ok = await DownloadProgressWindow.RunAsync(this, $"Downloading {entry.Name}…",
			async (ui, ct) => {
				exePath = category == InstanceCategory.Terraria
					? await DepotDownloaderService.DownloadTerrariaAsync(ui, entry, installDir, user, pass, ct)
					: await Downloader.InstallFromUrlAsync(ui, entry, category, installDir, ct);
				return exePath != null;
			});
		if (!ok || exePath == null) return false;

		InstanceManager.AddInstance(new InstanceRecord {
			Name        = entry.Name,
			Version     = entry.Version,
			Category    = category,
			InstallPath = installDir,
			ExePath     = exePath,
			LinkedTerrariaInstanceId = linkedTerrariaId,
		});
		return true;
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
