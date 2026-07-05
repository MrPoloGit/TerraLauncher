using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Metadata;

namespace TerraLauncher.Controls.Terraria;

// Scroll container with the Terraria-styled gold scrollbar. The custom
// Border-based bar bypasses Fluent's ScrollBar ControlTheme, which hides
// standalone scrollbars behind opacity animations.
public partial class TerrariaScrollBox : UserControl {
	private bool _dragging;
	private double _dragStartY;
	private double _dragStartOffset;

	// XAML children land inside the internal scroll viewer
	[Content]
	public object? ScrollContent {
		get => scrollViewer.Content;
		set => scrollViewer.Content = value;
	}

	public double ScrollSpeed {
		get => scrollViewer.ScrollSpeed;
		set => scrollViewer.ScrollSpeed = value;
	}

	public void ScrollToEnd() => scrollViewer.ScrollToEnd();

	public TerrariaScrollBox() {
		InitializeComponent();
		scrollViewer.ScrollSpeed = Config.ScrollSpeed;
		scrollViewer.LayoutUpdated += (_, _) => SyncScrollbar();

		scrollThumb.PointerPressed += OnThumbPressed;
		scrollThumb.PointerMoved += OnThumbMoved;
		scrollThumb.PointerReleased += OnThumbReleased;
		scrollTrack.PointerPressed += OnTrackPressed;
	}

	private void SyncScrollbar() {
		double max = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
		scrollTrack.IsVisible = max > 0;
		if (max <= 0 || scrollTrack.Bounds.Height <= 0) return;

		double trackH = scrollTrack.Bounds.Height;
		double ratio = scrollViewer.Viewport.Height / Math.Max(1, scrollViewer.Extent.Height);
		double thumbH = Math.Max(20, trackH * ratio);
		double range = trackH - thumbH;
		double pos = range > 0 ? (scrollViewer.Offset.Y / max) * range : 0;

		scrollThumb.Height = thumbH;
		scrollThumb.Margin = new Thickness(0, pos, 0, 0);
	}

	private void OnThumbPressed(object? sender, PointerPressedEventArgs e) {
		if (!e.GetCurrentPoint(scrollThumb).Properties.IsLeftButtonPressed) return;
		_dragging = true;
		_dragStartY = e.GetPosition(scrollTrack).Y;
		_dragStartOffset = scrollViewer.Offset.Y;
		e.Pointer.Capture(scrollThumb);
		e.Handled = true;
	}

	private void OnThumbMoved(object? sender, PointerEventArgs e) {
		if (!_dragging) return;
		double dy = e.GetPosition(scrollTrack).Y - _dragStartY;
		double max = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
		double trackH = scrollTrack.Bounds.Height;
		double thumbH = scrollThumb.Bounds.Height;
		double range = trackH - thumbH;
		if (range <= 0) return;
		double newOffset = Math.Clamp(_dragStartOffset + (dy / range) * max, 0, max);
		scrollViewer.Offset = new Vector(scrollViewer.Offset.X, newOffset);
		e.Handled = true;
	}

	private void OnThumbReleased(object? sender, PointerReleasedEventArgs e) {
		_dragging = false;
		e.Pointer.Capture(null);
	}

	private void OnTrackPressed(object? sender, PointerPressedEventArgs e) {
		// Bubbled from thumb — handled there already
		if (ReferenceEquals(e.Source, scrollThumb)) return;
		double y = e.GetPosition(scrollTrack).Y;
		double max = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
		double trackH = scrollTrack.Bounds.Height;
		double thumbH = scrollThumb.Bounds.Height;
		double range = Math.Max(1, trackH - thumbH);
		double newOffset = Math.Clamp(((y - thumbH / 2) / range) * max, 0, max);
		scrollViewer.Offset = new Vector(scrollViewer.Offset.X, newOffset);
	}
}
