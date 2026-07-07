using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace TerraLauncher.Controls.Terraria {
	// ToggleButton version of TerrariaButton's frame rendering, used as the
	// drop-down toggle in the themed ComboBox template.
	public class TerrariaToggleButton : ToggleButton {
		static TerrariaToggleButton() {
			DefaultStyleKeyProperty.OverrideMetadata(typeof(TerrariaToggleButton),
					   new FrameworkPropertyMetadata(typeof(TerrariaToggleButton)));
		}

		bool inside = false;

		protected override void OnRender(DrawingContext d) {
			CroppedFrame frame = CroppedFrames.ButtonFrame;
			if (IsChecked == true)
				frame = CroppedFrames.ButtonFrameDark;
			else if (inside)
				frame = CroppedFrames.ButtonFrameLight;

			DrawCropped.DrawFrame(d, frame, ActualWidth, ActualHeight);
			base.OnRender(d);
		}

		private void OnMouseEnter(object sender, MouseEventArgs e) {
			inside = true;
			Sounds.PlayTick();
			InvalidateVisual();
		}
		private void OnMouseLeave(object sender, MouseEventArgs e) {
			inside = false;
			InvalidateVisual();
		}

		public override void OnApplyTemplate() {
			this.MouseEnter += OnMouseEnter;
			this.MouseLeave += OnMouseLeave;
			Checked += (s, e) => InvalidateVisual();
			Unchecked += (s, e) => InvalidateVisual();
		}
	}
}
