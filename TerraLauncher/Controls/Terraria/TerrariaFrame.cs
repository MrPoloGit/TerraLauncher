using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TerraLauncher.Controls.Terraria {
	// Static (non-interactive) variant of TerrariaButton's frame rendering — used
	// to give a plain container (e.g. the main window's search box) the same
	// pixel-art button-frame look without wiring up hover/press mouse capture,
	// which would otherwise interfere with a child TextBox's own mouse handling.
	public class TerrariaFrame : ContentControl {
		static TerrariaFrame() {
			DefaultStyleKeyProperty.OverrideMetadata(typeof(TerrariaFrame),
					   new FrameworkPropertyMetadata(typeof(TerrariaFrame)));
		}

		protected override void OnRender(DrawingContext d) {
			DrawCropped.DrawFrame(d, CroppedFrames.ButtonFrame, ActualWidth, ActualHeight);
			base.OnRender(d);
		}
	}
}
