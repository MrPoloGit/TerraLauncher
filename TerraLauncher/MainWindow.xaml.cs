using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TerraLauncher.Windows;
using TerraLauncher.Properties;
using Microsoft.Win32;
using System.Windows.Media.Animation;
using TerraLauncher.Controls.Terraria;
using TerraLauncher.Setups;
using TerraLauncher.Util;
using System.ComponentModel;

namespace TerraLauncher {
	/**<summary>The main window running Terraria Item Modifier.</summary>*/
	public partial class MainWindow : Window {
		//=========== MEMBERS ============
		#region Members

		bool loaded = false;
		Stack<TerrariaSetupList> gameStack = new Stack<TerrariaSetupList>();

		private static readonly string[] FilterNames =
			{ "All Instances", "Terraria", "tModLoader", "tAPI", "tConfig", "Stand Alone", "Custom" };
		private static readonly GameCategory?[] FilterCategories =
			{ null, GameCategory.Terraria, GameCategory.TModLoader, GameCategory.TAPI,
			  GameCategory.TConfig, GameCategory.StandAlone, GameCategory.Custom };

		#endregion
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the main window.</summary>*/
		public MainWindow() {
			InitializeComponent();

			comboFilter.ItemsSource = FilterNames;
			comboFilter.SelectedIndex = 0;

			LoadSettings();
			Opacity = 0;

			// Setup Config Path key for Trigger Tool integration
			RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true);
			key = key.CreateSubKey("TriggersToolsGames");
			key = key.CreateSubKey("TerraLauncher");
			key.SetValue("ConfigPath", Config.ConfigPath);
		}

		#endregion
		//=========== SETTINGS ===========
		#region Settings

		/**<summary>Loads the application settings.</summary>*/
		private void LoadSettings() {
			Config.LoadConfig(this);
			EnsureSteamTModLoaderEntry();

			LoadSetups();

			int width = Settings.Default.WindowWidth;
			int height = Settings.Default.WindowHeight;
			if (width >= MinWidth)
				Width = width;
			if (height >= MinHeight)
				Height = height;
		}

		// Steam installs tModLoader as its own separate app (1281930) next to
		// Terraria — auto-add it to the list the same way vanilla Terraria's
		// Steam install is auto-detected, instead of leaving the user to find
		// and link the launch script by hand.
		private static void EnsureSteamTModLoaderEntry() {
			string path = TerrariaLocator.TModLoaderPath;
			if (string.IsNullOrEmpty(path)) return;
			if (FolderContainsExe(Config.Games, path)) return;

			Config.Games.Entries.Add(new Game {
				Name     = "tModLoader",
				ExePath  = path,
				Category = GameCategory.TModLoader,
				Icon     = "TreeJungle",
				Details  = "Steam",
			});
			Config.Modified = true;
			Config.SaveConfig();
		}

		private static bool FolderContainsExe(SetupFolder folder, string path) {
			foreach (var entry in folder.Entries) {
				if (entry is Game g && string.Equals(g.ExePath, path, StringComparison.OrdinalIgnoreCase)) return true;
				if (entry is SetupFolder sub && FolderContainsExe(sub, path)) return true;
			}
			return false;
		}
		/**<summary>Saves the application settings.</summary>*/
		private void SaveSettings() {
			if (Config.Modified)
				Config.SaveConfig();

			Settings.Default.WindowWidth = (int)Width;
			Settings.Default.WindowHeight = (int)Height;
			Settings.Default.Save();
		}

		#endregion
		//=========== HELPERS ============
		#region Helpers

		private void ReloadSetups() {
			gridGames.Children.Clear();
			gameStack.Clear();
			LoadSetups();
		}

		private void LoadSetups() {
			TerrariaSetupList setupList = new TerrariaSetupList();
			setupList.PopulateList(Config.Games, (folder) => {
				NavigateForward(gridGames, gameStack, folder);
			});
			gameStack.Push(setupList);
			gridGames.Children.Add(setupList);
			ApplyFilter();
		}

		private void NavigateForward(Grid gridList, Stack<TerrariaSetupList> stack, SetupFolder folder) {
			TerrariaSetupList setupList = new TerrariaSetupList();
			setupList.PopulateList(folder, (folder2) => {
				NavigateForward(gridList, stack, folder2);
			}, () => {
				NavigateBack(gridList, stack);
			});
			var last = stack.Peek();
			stack.Push(setupList);
			gridList.Children.Add(setupList);
			if (!Config.DisableTransitions) {
				last.LeaveFolder(false, gridList.ActualWidth);
				setupList.EnterFolder(false, gridList.ActualWidth);
			}
			else {
				last.Visibility = Visibility.Hidden;
			}
			ApplyFilter();
		}
		private void NavigateBack(Grid gridList, Stack<TerrariaSetupList> stack) {
			var last = stack.Pop();
			var setupList = stack.Peek();
			if (!Config.DisableTransitions) {
				last.LeaveFolder(true, gridList.ActualWidth);
				setupList.EnterFolder(true, gridList.ActualWidth);
			}
			else {
				gridList.Children.Remove(last);
				setupList.Visibility = Visibility.Visible;
			}
			ApplyFilter();
		}

		#endregion
		//=========== SEARCH =============
		#region Search

		private void OnSearchChanged(object sender, TextChangedEventArgs e) {
			textBlockSearchWatermark.Visibility = (textBoxSearch.Text.Length == 0) ? Visibility.Visible : Visibility.Collapsed;
			ApplyFilter();
		}

		private void OnFilterChanged(object sender, SelectionChangedEventArgs e) {
			ApplyFilter();
		}

		// Restores/filters whichever TerrariaSetupList is currently on top of the
		// navigation stack: either the normal folder view, or a flattened list
		// (search text and/or category) spanning every folder.
		private void ApplyFilter() {
			if (gameStack.Count == 0)
				return;
			var top = gameStack.Peek();
			string query = (textBoxSearch.Text ?? "").Trim();
			int filterIndex = Math.Max(0, comboFilter.SelectedIndex);
			GameCategory? category = FilterCategories[Math.Min(filterIndex, FilterCategories.Length - 1)];

			if (query.Length == 0 && category == null) {
				var folder = top.Folder;
				if (folder.Parent == null)
					top.PopulateList(folder, (sub) => NavigateForward(gridGames, gameStack, sub));
				else
					top.PopulateList(folder, (sub) => NavigateForward(gridGames, gameStack, sub), () => NavigateBack(gridGames, gameStack));
				return;
			}

			List<Setup> matches = new List<Setup>();
			WalkForMatches(Config.Games, query, category, matches);

			string what = category != null ? FilterNames[filterIndex] + " instances" : "instances";
			string emptyMessage = query.Length > 0
				? "No " + what + " match \"" + query + "\"."
				: "No " + what + " installed.";
			top.PopulateFlat(matches, emptyMessage);
		}

		private static void WalkForMatches(SetupFolder folder, string query, GameCategory? category, List<Setup> matches) {
			foreach (var entry in folder.Entries) {
				if (entry is SetupFolder subFolder) {
					WalkForMatches(subFolder, query, category, matches);
				}
				else if (entry is Setup setup) {
					if (query.Length > 0) {
						bool nameMatch = setup.Name != null && setup.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
						bool detailsMatch = setup.Details != null && setup.Details.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
						if (!nameMatch && !detailsMatch)
							continue;
					}
					if (category != null && !(setup is Game game && game.Category == category))
						continue;
					matches.Add(setup);
				}
			}
		}

		#endregion
		//============ EVENTS ============
		#region Events

		private void OnWindowLoaded(object sender, RoutedEventArgs e) {
			Sounds.PlayOpen();
			var anim = new DoubleAnimation(0, 1, (Duration)TimeSpan.FromSeconds(0.4));
			anim.Completed += (s, _) => { loaded = true; } ;
			this.BeginAnimation(UIElement.OpacityProperty, anim);
		}
		private void OnWindowClosing(object sender, CancelEventArgs e) {
			SaveSettings();
			this.Closing -= OnWindowClosing;
			e.Cancel = true;
			var anim = new DoubleAnimation(0, (Duration)TimeSpan.FromSeconds(0.5));
			anim.Completed += (s, _) => this.Close();
			this.BeginAnimation(UIElement.OpacityProperty, anim);
		}

		#endregion

		private void OnPreviewKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Back || e.Key == Key.BrowserBack) {
				if (!textBoxSearch.IsFocused && gameStack.Count > 1)
					NavigateBack(gridGames, gameStack);
			}
		}

		private void OnEditSetups(object sender, MouseButtonEventArgs e) {
			if (SettingsWindow.ShowDialog(this)) {
				ReloadSetups();
			}
		}

		private void OnAddInstance(object sender, MouseButtonEventArgs e) {
			if (AddInstanceWindow.ShowDialog(this)) {
				ReloadSetups();
			}
		}
	}
}
