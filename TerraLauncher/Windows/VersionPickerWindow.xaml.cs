using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TerraLauncher.Instances;
using TerraLauncher.Setups;

namespace TerraLauncher.Windows {
	public partial class VersionPickerWindow : Window {
		private readonly GameCategory category;
		private int currentPage = 1;
		private bool hasNextPage = false;
		// Full, unfiltered set of entries from the last fetch — Stand Alone bundles
		// several unrelated standalone games under one category, so type/search
		// filtering happens client-side over this rather than re-fetching.
		private List<VersionEntry> allEntries = new List<VersionEntry>();

		private static readonly string[] StandAloneTypes = {
			"All Types", "Avalon", "Exxo Avalon", "N Terraria", "Ulterraria", "Prepare to Die",
		};

		// Rows reserve this much right margin so their content doesn't render
		// under the scrollbar (ScrollContentPresenter spans both the content and
		// scrollbar columns). When the scrollbar auto-hides because everything
		// fits, that reserve must drop to 0 or the list looks lopsided.
		private const double RowRightMarginWithScrollbar = 26;

		public VersionPickerWindow(GameCategory category) {
			InitializeComponent();
			this.category = category;
			labelCategory.Text = CategoryLabel(category);
			// Only tModLoader's GitHub releases are paginated — every other
			// category is a single, already-complete local/embedded list.
			panelPaging.Visibility = category == GameCategory.TModLoader ? Visibility.Visible : Visibility.Collapsed;

			if (category == GameCategory.StandAlone) {
				frameTypeSearch.Visibility = Visibility.Visible;
				comboType.Visibility = Visibility.Visible;
				borderList.Margin = new Thickness(10, 90, 10, 44);
				comboType.ItemsSource = StandAloneTypes;
				comboType.SelectedIndex = 0;
			}

			ContentRendered += async (s, e) => await LoadVersionsAsync();
			scrollViewer.ScrollChanged += (s, e) => UpdateRowMargins();
		}

		private void UpdateRowMargins() {
			double right = scrollViewer.ScrollableHeight > 0 ? RowRightMarginWithScrollbar : 0;
			foreach (object child in versionList.Children) {
				if (child is FrameworkElement element) {
					Thickness m = element.Margin;
					if (m.Right != right)
						element.Margin = new Thickness(m.Left, m.Top, right, m.Bottom);
				}
			}
		}

		private static string CategoryLabel(GameCategory c) {
			switch (c) {
			case GameCategory.Terraria:   return "Terraria Versions";
			case GameCategory.TModLoader: return "tModLoader";
			case GameCategory.TAPI:       return "tAPI";
			case GameCategory.TConfig:    return "tConfig";
			case GameCategory.StandAlone: return "Stand Alone";
			default:                     return c.ToString();
			}
		}

		private async void OnRefresh(object sender, RoutedEventArgs e) => await LoadVersionsAsync();

		private async void OnPreviousPage(object sender, RoutedEventArgs e) {
			if (currentPage <= 1) return;
			currentPage--;
			await LoadVersionsAsync();
		}

		private async void OnNextPage(object sender, RoutedEventArgs e) {
			if (!hasNextPage) return;
			currentPage++;
			await LoadVersionsAsync();
		}

		private async void OnPageInputKeyDown(object sender, KeyEventArgs e) {
			if (e.Key != Key.Enter) return;
			await JumpToTypedPageAsync();
			Keyboard.ClearFocus();
		}

		private async void OnPageInputLostFocus(object sender, RoutedEventArgs e) => await JumpToTypedPageAsync();

		private async Task JumpToTypedPageAsync() {
			int parsed;
			if (!int.TryParse(textBoxPage.Text, out parsed) || parsed < 1) {
				textBoxPage.Text = currentPage.ToString();
				return;
			}
			if (parsed == currentPage) return;
			currentPage = parsed;
			await LoadVersionsAsync();
		}

		private async Task LoadVersionsAsync() {
			labelStatus.Text = "Loading...";
			versionList.Children.Clear();

			VersionPage page;
			try {
				page = await VersionSource.GetVersionsAsync(category, currentPage);
			}
			catch (Exception ex) {
				labelStatus.Text = "Failed to load: " + ex.Message;
				return;
			}

			hasNextPage = page.HasNextPage;
			textBoxPage.Text = currentPage.ToString();
			buttonPrevious.IsEnabled = currentPage > 1;
			buttonNext.IsEnabled = hasNextPage;

			allEntries = page.Entries;
			RenderFilteredEntries();
		}

		private void OnVersionSearchChanged(object sender, TextChangedEventArgs e) {
			labelSearchWatermark.Visibility = textBoxVersionSearch.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
			RenderFilteredEntries();
		}
		private void OnTypeFilterChanged(object sender, SelectionChangedEventArgs e) => RenderFilteredEntries();

		private void RenderFilteredEntries() {
			versionList.Children.Clear();

			IEnumerable<VersionEntry> filtered = allEntries;

			if (category == GameCategory.StandAlone) {
				string type = comboType.SelectedItem as string;
				if (!string.IsNullOrEmpty(type) && type != "All Types")
					filtered = filtered.Where(v => v.Type == type);
			}

			string search = (textBoxVersionSearch?.Text ?? "").Trim();
			if (search.Length > 0)
				filtered = filtered.Where(v => v.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);

			List<VersionEntry> results = filtered.ToList();
			if (allEntries.Count == 0) {
				labelStatus.Text = "No versions found.";
				return;
			}
			if (results.Count == 0) {
				labelStatus.Text = "No versions match your search.";
				return;
			}

			foreach (var entry in results)
				versionList.Children.Add(BuildRow(entry));

			labelStatus.Text = results.Count + " version(s)";
		}

		private UIElement BuildRow(VersionEntry entry) {
			Border border = new Border {
				Background      = new SolidColorBrush(Color.FromRgb(0x32, 0x33, 0x81)),
				BorderBrush     = new SolidColorBrush(Color.FromRgb(0x14, 0x14, 0x3A)),
				BorderThickness = new Thickness(1),
				CornerRadius    = new CornerRadius(3),
				Padding         = new Thickness(10, 8, 10, 8),
				Margin          = new Thickness(0, 0, 0, 6),
			};

			Grid grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			StackPanel info = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
			info.Children.Add(new TextBlock { Text = entry.Name, FontSize = 16, Foreground = Brushes.White });
			if (!string.IsNullOrEmpty(entry.Description)) {
				info.Children.Add(new TextBlock {
					Text = entry.Description, FontSize = 11,
					Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xCC)),
					TextWrapping = TextWrapping.Wrap,
				});
			}

			StackPanel meta = new StackPanel { Orientation = Orientation.Horizontal };
			if (!string.IsNullOrEmpty(entry.Date))
				meta.Children.Add(new TextBlock { Text = entry.Date, FontSize = 10, Foreground = Brushes.Gray, Margin = new Thickness(0, 4, 10, 0) });
			if (!string.IsNullOrEmpty(entry.Author))
				meta.Children.Add(new TextBlock { Text = "by " + entry.Author, FontSize = 10, Foreground = Brushes.Gray, Margin = new Thickness(0, 4, 0, 0) });
			if (!string.IsNullOrEmpty(entry.RequiresTerrariaVersion)) {
				meta.Children.Add(new TextBlock {
					Text = "Requires Terraria " + entry.RequiresTerrariaVersion, FontSize = 10,
					Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xAA, 0x44)), Margin = new Thickness(10, 4, 0, 0),
				});
			}
			if (meta.Children.Count > 0) info.Children.Add(meta);

			Grid.SetColumn(info, 0);
			grid.Children.Add(info);

			bool installed = FindInstalledGame(category, entry.Version, entry.Type) != null;

			Button button = new Button { Padding = new Thickness(14, 4, 14, 4), VerticalAlignment = VerticalAlignment.Center, MinWidth = 90 };
			if (installed) {
				button.Content = "Installed";
				button.IsEnabled = false;
			}
			else {
				button.Content = "Download";
				button.Click += async (s, e) => await OnDownloadClicked(entry);
			}
			Grid.SetColumn(button, 1);
			grid.Children.Add(button);

			if (installed)
				border.Opacity = 0.55;

			border.Child = grid;
			return border;
		}

		private async Task OnDownloadClicked(VersionEntry entry) {
			if (!string.IsNullOrEmpty(entry.RequiresTerrariaVersion)) {
				string required = entry.RequiresTerrariaVersion;
				Game existing = FindInstalledGame(GameCategory.Terraria, required);
				if (existing == null) {
					MessageBoxResult result = TriggerMessageBox.Show(this, MessageIcon.Question,
						entry.Name + " requires Terraria " + required + " which is not installed.\n\n"
						+ "Download it now? (Requires Steam login)\n"
						+ "Choose No to install " + entry.Name + " without it.",
						"Dependency Required", MessageBoxButton.YesNoCancel);
					if (result == MessageBoxResult.Cancel) return;
					if (result == MessageBoxResult.Yes) {
						VersionPage terrariaPage = await VersionSource.GetVersionsAsync(GameCategory.Terraria);
						VersionEntry dep = terrariaPage.Entries.FirstOrDefault(v => v.Version == required)
							?? new VersionEntry { Name = "Terraria " + required, Version = required, DepotId = 105601 };
						if (!await DownloadEntryAsync(dep, GameCategory.Terraria)) return;
					}
				}
			}

			if (await DownloadEntryAsync(entry, category)) {
				DialogResult = true;
				Close();
			}
		}

		private async Task<bool> DownloadEntryAsync(VersionEntry entry, GameCategory cat) {
			// StandAlone bundles several unrelated games (Avalon, N Terraria, ...)
			// under one category, so their install/save folders (and "already
			// installed" check) are keyed off VersionEntry.Type as well as version.
			string label = cat == GameCategory.StandAlone ? entry.Type : null;

			if (FindInstalledGame(cat, entry.Version, label) != null) {
				TriggerMessageBox.Show(this, MessageIcon.Info, entry.Name + " is already installed.", "Already Installed");
				return false;
			}

			// Versions predating Steam's depot/manifest system (pre-1.2ish) have no
			// manifestId and instead carry a direct archive Url, so they install
			// like any other category instead of going through DepotDownloader.
			bool useSteam = cat == GameCategory.Terraria && string.IsNullOrEmpty(entry.Url);

			string username = "", password = "";
			if (useSteam) {
				var login = TextPromptWindow.ShowLoginAsync(this, "Steam Login",
					"Steam credentials are required to download Terraria.\n"
					+ "They are passed directly to DepotDownloader and never saved.",
					"Steam Username", "Steam Password");
				if (login == null || string.IsNullOrWhiteSpace(login.Value.User)) return false;
				username = login.Value.User;
				password = login.Value.Pass;
			}
			else if (string.IsNullOrEmpty(entry.Url)) {
				TriggerMessageBox.Show(this, MessageIcon.Error, "No download is available for " + entry.Name + " yet.", "Not Available");
				return false;
			}

			string installDir = InstancePaths.GetInstallDirForVersion(cat, entry.Version, label);

			string exePath = null;
			string modBuilderPath = null;
			bool ok = await DownloadProgressWindow.RunAsync(this, "Downloading " + entry.Name + "...", async (ui, ct) => {
				if (useSteam) {
					exePath = await DepotDownloaderService.DownloadTerrariaAsync(ui, entry, installDir, username, password, ct);
				}
				else {
					var result = await Downloader.InstallFromUrlAsync(ui, entry, cat, installDir, ct);
					exePath = result.ExePath;
					modBuilderPath = result.ModBuilderPath;
				}
				return exePath != null;
			});
			if (!ok || exePath == null) return false;

			Game game = new Game {
				Name     = entry.Name,
				Category = cat,
				Version  = entry.Version,
				ExePath  = exePath,
				Details  = entry.Version,
				Icon     = DefaultIconFor(cat),
				SubType  = label ?? "",
			};
			if ((cat == GameCategory.TAPI || cat == GameCategory.TConfig) && !string.IsNullOrEmpty(modBuilderPath))
				game.ModBuilderPath = modBuilderPath;

			// Give every downloaded instance its own Worlds/Players (and, for
			// mod-capable categories, Mods) folder under Documents instead of
			// sharing one global save location, so installing multiple versions
			// never mixes their saves.
			string saveDir = InstancePaths.GetSaveDataDirForVersion(cat, entry.Version, label);
			Directory.CreateDirectory(Path.Combine(saveDir, "Worlds"));
			Directory.CreateDirectory(Path.Combine(saveDir, "Players"));
			if (cat == GameCategory.TModLoader || cat == GameCategory.TAPI || cat == GameCategory.TConfig)
				Directory.CreateDirectory(Path.Combine(saveDir, "Mods"));
			game.SaveDirectory = saveDir;

			Config.Games.Entries.Add(game);
			Config.Modified = true;
			Config.SaveConfig();
			return true;
		}

		// Keeps the main instance list visually distinct by category, matching
		// the Add Instance tile art (tModLoader gets the jungle-tree variant).
		private static string DefaultIconFor(GameCategory cat) {
			switch (cat) {
			case GameCategory.TModLoader: return "TreeJungle";
			case GameCategory.TAPI:       return "TAPI";
			case GameCategory.TConfig:    return "TConfig";
			case GameCategory.StandAlone: return "TreeCorruptionHallow";
			default:                     return "Tree";
			}
		}

		// A Config entry alone isn't enough — if the user deleted the install
		// folder by hand, the record can be stale, so this checks the actual
		// executable is still on disk before treating a version as "installed".
		private static Game FindInstalledGame(GameCategory cat, string version, string label = null) {
			if (string.IsNullOrEmpty(version)) return null;
			return Walk(Config.Games, g => g.Category == cat && g.Version == version
				&& g.SubType == (label ?? "") && File.Exists(g.ExePath));
		}

		private static Game Walk(SetupFolder folder, Func<Game, bool> predicate) {
			foreach (var entry in folder.Entries) {
				if (entry is SetupFolder sub) {
					Game found = Walk(sub, predicate);
					if (found != null) return found;
				}
				else if (entry is Game g && predicate(g)) {
					return g;
				}
			}
			return null;
		}

		public static bool ShowDialog(Window owner, GameCategory category) {
			VersionPickerWindow w = new VersionPickerWindow(category);
			w.Owner = owner;
			var result = w.ShowDialog();
			return result.HasValue && result.Value;
		}
	}
}
