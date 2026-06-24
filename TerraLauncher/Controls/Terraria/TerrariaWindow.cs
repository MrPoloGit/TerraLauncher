using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;

namespace TerraLauncher.Controls.Terraria;

public class TerrariaWindow : ContentControl {
	public override void Render(DrawingContext context) {
		CroppedFrames.EnsureInitialized();
		DrawCropped.DrawFrame(context, CroppedFrames.WindowFrame, Bounds.Width, Bounds.Height);
		base.Render(context);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e) {
		base.OnApplyTemplate(e);

		var closeButton = e.NameScope.Find<TerrariaButton>("closeButton");
		var minimizeButton = e.NameScope.Find<TerrariaButton>("minimizeButton");
		var titleBar = e.NameScope.Find<Panel>("titleBar");

		if (closeButton != null)
			closeButton.Click += (_, _) => {
				Sounds.PlayClose();
				(TopLevel.GetTopLevel(this) as Window)?.Close();
			};
		if (minimizeButton != null)
			minimizeButton.Click += (_, _) => {
				Sounds.PlayClose();
				if (TopLevel.GetTopLevel(this) is Window w)
					w.WindowState = WindowState.Minimized;
			};
		if (titleBar != null)
			titleBar.PointerPressed += OnDragWindow;

		AttachResizeGrip(e, "leftSizeGrip", WindowEdge.West);
		AttachResizeGrip(e, "rightSizeGrip", WindowEdge.East);
		AttachResizeGrip(e, "topSizeGrip", WindowEdge.North);
		AttachResizeGrip(e, "bottomSizeGrip", WindowEdge.South);
		AttachResizeGrip(e, "topLeftSizeGrip", WindowEdge.NorthWest);
		AttachResizeGrip(e, "topRightSizeGrip", WindowEdge.NorthEast);
		AttachResizeGrip(e, "bottomLeftSizeGrip", WindowEdge.SouthWest);
		AttachResizeGrip(e, "bottomRightSizeGrip", WindowEdge.SouthEast);
	}

	private void AttachResizeGrip(TemplateAppliedEventArgs e, string name, WindowEdge edge) {
		var grip = e.NameScope.Find<Control>(name);
		if (grip != null)
			grip.PointerPressed += (_, pe) => {
				if (TopLevel.GetTopLevel(this) is Window w)
					w.BeginResizeDrag(edge, pe);
			};
	}

	private void OnDragWindow(object? sender, PointerPressedEventArgs e) {
		if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
			(TopLevel.GetTopLevel(this) as Window)?.BeginMoveDrag(e);
	}
}
