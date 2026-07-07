using System.Windows;
using System.Windows.Input;
using TerraLauncher.Setups;

namespace TerraLauncher.Windows {
	/// <summary>
	/// Interaction logic for AddInstanceWindow.xaml
	/// </summary>
	public partial class AddInstanceWindow : Window {
		public AddInstanceWindow() {
			InitializeComponent();
		}

		private void OnTerraria(object sender, MouseButtonEventArgs e) => OpenPicker(GameCategory.Terraria);
		private void OnTModLoader(object sender, MouseButtonEventArgs e) => OpenPicker(GameCategory.TModLoader);
		private void OnTAPI(object sender, MouseButtonEventArgs e) => OpenPicker(GameCategory.TAPI);
		private void OnTConfig(object sender, MouseButtonEventArgs e) => OpenPicker(GameCategory.TConfig);
		private void OnStandAlone(object sender, MouseButtonEventArgs e) => OpenPicker(GameCategory.StandAlone);

		private void OnCustom(object sender, MouseButtonEventArgs e) {
			Game game = new Game();
			game.Category = GameCategory.Custom;

			if (EditGameWindow.ShowDialog(this, game)) {
				Config.Games.Entries.Add(game);
				Config.Modified = true;
				Config.SaveConfig();
				DialogResult = true;
			}
		}

		private void OpenPicker(GameCategory category) {
			if (VersionPickerWindow.ShowDialog(this, category))
				DialogResult = true;
		}

		public static bool ShowDialog(Window owner) {
			AddInstanceWindow window = new AddInstanceWindow();
			window.Owner = owner;
			var result = window.ShowDialog();
			return result.HasValue && result.Value;
		}
	}
}
