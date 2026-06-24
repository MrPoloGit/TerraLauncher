using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace TerraLauncher.Controls.Terraria;

public class TerrariaButton : Button {
	public override void Render(DrawingContext context) {
		CroppedFrames.EnsureInitialized();
		var frame = IsPressed ? CroppedFrames.ButtonFrameDark
			: IsPointerOver ? CroppedFrames.ButtonFrameLight
			: CroppedFrames.ButtonFrame;
		DrawCropped.DrawFrame(context, frame, Bounds.Width, Bounds.Height);
	}

	protected override void OnPointerEntered(PointerEventArgs e) {
		base.OnPointerEntered(e);
		Sounds.PlayTick();
		InvalidateVisual();
	}

	protected override void OnPointerExited(PointerEventArgs e) {
		base.OnPointerExited(e);
		InvalidateVisual();
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e) {
		base.OnPointerPressed(e);
		InvalidateVisual();
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e) {
		base.OnPointerReleased(e);
		InvalidateVisual();
	}
}
