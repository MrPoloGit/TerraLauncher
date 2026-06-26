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
		// Delegate to Avalonia's native handler first — it correctly handles macOS trackpad momentum.
		// Then nudge the offset further when ScrollSpeed != 1 to apply the multiplier.
		var before = Offset.Y;
		base.OnPointerWheelChanged(e);
		if (System.Math.Abs(ScrollSpeed - 1.0) > 0.01) {
			double extra = (Offset.Y - before) * (ScrollSpeed - 1.0);
			double newOffset = System.Math.Clamp(Offset.Y + extra, 0, ScrollBarMaximum.Y);
			Offset = new Vector(Offset.X, newOffset);
		}
	}
}
