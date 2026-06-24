using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace TerraLauncher.Controls;

public class SpeedScrollViewer : ScrollViewer {
	public static readonly StyledProperty<double> ScrollSpeedProperty =
		AvaloniaProperty.Register<SpeedScrollViewer, double>(nameof(ScrollSpeed), 1.0);

	public double ScrollSpeed {
		get => GetValue(ScrollSpeedProperty);
		set => SetValue(ScrollSpeedProperty, value);
	}

	protected override void OnPointerWheelChanged(PointerWheelEventArgs e) {
		double delta = e.Delta.Y * ScrollSpeed * 50;
		double newOffset = Offset.Y - delta;
		newOffset = System.Math.Clamp(newOffset, 0, ScrollBarMaximum.Y);
		Offset = new Vector(Offset.X, newOffset);
		e.Handled = true;
	}
}
