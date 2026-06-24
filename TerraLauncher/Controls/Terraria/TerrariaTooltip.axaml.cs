using Avalonia;
using Avalonia.Controls;

namespace TerraLauncher.Controls.Terraria;

public partial class TerrariaTooltip : UserControl {
	public static readonly StyledProperty<string> TextProperty =
		AvaloniaProperty.Register<TerrariaTooltip, string>(nameof(Text), "Tooltip");

	public string Text {
		get => GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	public TerrariaTooltip() {
		InitializeComponent();
	}
}
