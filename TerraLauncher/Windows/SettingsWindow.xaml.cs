using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TerraLauncher.Util;

namespace TerraLauncher.Windows {
	/// <summary>
	/// Interaction logic for EditSetupsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : Window {
		
		public SettingsWindow() {
			InitializeComponent();

			checkBoxCloseGame.IsChecked = Config.CloseOnGameLaunch;

			checkBoxDisableTransitions.IsChecked = Config.DisableTransitions;
			checkBoxMuted.IsChecked = Config.Muted;
			checkBoxIntegration.IsChecked = Config.Integration;

			spinnerScrollSpeed.Value = (int)(Config.ScrollSpeed * 100);
		}

		public static bool ShowDialog(Window owner) {
			SettingsWindow window = new SettingsWindow();
			window.Owner = owner;
			var result = window.ShowDialog();

			if (result.HasValue && result.Value) {
				Config.CloseOnGameLaunch = window.checkBoxCloseGame.IsChecked.Value;

				Config.DisableTransitions = window.checkBoxDisableTransitions.IsChecked.Value;
				Config.Muted = window.checkBoxMuted.IsChecked.Value;
				Config.Integration = window.checkBoxIntegration.IsChecked.Value;
				Config.ScrollSpeed = (double)window.spinnerScrollSpeed.Value / 100.0;

				Config.SaveConfig();
				return true;
			}
			return false;
		}

		private void OnOKClicked(object sender, RoutedEventArgs e) {
			DialogResult = true;
		}

		//============ EVENTS ============
		#region Events
		//--------------------------------
		#region Menu Items
		
		private void OnAbout(object sender, RoutedEventArgs e) {
			AboutWindow.Show(this);
		}
		private void OnHelp(object sender, RoutedEventArgs e) {
			Process.Start(new ProcessStartInfo("https://github.com/trigger-death/TerraLauncher/wiki") { UseShellExecute = true });
		}
		private void OnCredits(object sender, RoutedEventArgs e) {
			CreditsWindow.Show(this);
		}
		private void OnViewOnGitHub(object sender, RoutedEventArgs e) {
			Process.Start(new ProcessStartInfo("https://github.com/trigger-death/TerraLauncher") { UseShellExecute = true });
		}

		#endregion
		//--------------------------------
		#endregion
	}
}
