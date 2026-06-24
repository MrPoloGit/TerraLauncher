using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using TerraLauncher.Setups;

namespace TerraLauncher.Controls.Terraria;

public partial class TerrariaSetupOptionButton : UserControl {
	private readonly SetupOption _option;

	public TerrariaSetupOptionButton(SetupOption option) {
		InitializeComponent();
		_option = option;
		tooltip.Text = option.Tooltip;
		var bitmap = Setup.GetOptionIcon(option.Icon);
		if (bitmap != null) {
			image.Source = bitmap;
			image.Width = System.Math.Min(28, bitmap.PixelSize.Width);
			image.Height = System.Math.Min(28, bitmap.PixelSize.Height);
		}
	}

	private void OnButton(object? sender, RoutedEventArgs e) => _option.Action();

	public void Action() => _option.Action();
}
