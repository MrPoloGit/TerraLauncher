using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;
using TerraLauncher.Setups;

namespace TerraLauncher.Controls.Terraria;

public partial class TerrariaSetupList : UserControl {
	private const double FolderTime = 0.4;
	private const double TabTime = 0.3;

	private Action<SetupFolder>? _navigateForward;
	private Action? _navigateBack;
	private SetupFolder? _folder;

	private bool _dragging;
	private double _dragStartY;
	private double _dragStartOffset;

	public SetupFolder? Folder => _folder;

	public TerrariaSetupList() {
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

	public void PopulateList(SetupFolder folder, Action<SetupFolder> navigateForward, Action? navigateBack = null) {
		_navigateForward = navigateForward;
		_navigateBack = navigateBack;
		_folder = folder;
		list.Children.Clear();
		if (folder.Parent != null && navigateBack != null) {
			list.Children.Add(new TerrariaSetupFolder(folder.Parent, true, navigateBack));
		}
		foreach (var entry in folder.Entries) {
			if (entry is SetupFolder sub) {
				list.Children.Add(new TerrariaSetupFolder(sub, false, () => navigateForward(sub)));
			}
			else if (entry is Setup setup) {
				list.Children.Add(new TerrariaSetupEntry(setup));
			}
		}
	}

	public async void EnterFolder(bool back, double width) {
		IsEnabled = false;
		IsVisible = true;
		await AnimateMargin(list, fromThickness: MakeThickness(back, true, false, width, FolderTime),
			toThickness: new Thickness(0), duration: FolderTime);
		IsEnabled = true;
	}

	public async void LeaveFolder(bool back, double width) {
		IsEnabled = false;
		await AnimateMargin(list, fromThickness: new Thickness(0),
			toThickness: MakeThickness(back, false, false, width, FolderTime),
			duration: FolderTime);
		IsEnabled = true;
		IsVisible = false;
		if (back && Parent is Panel p)
			p.Children.Remove(this);
	}

	public async void EnterTab(bool back, double width, bool middlePass) {
		IsEnabled = false;
		await AnimateMargin(list, MakeThickness(back, true, true, width, TabTime, middlePass), new Thickness(0),
			(TabTime * (middlePass ? 1.5 : 1)));
		IsEnabled = true;
	}

	public async void LeaveTab(bool back, double width, bool middlePass, Action? updateTabs) {
		IsEnabled = false;
		await AnimateMargin(list, new Thickness(0), MakeThickness(back, false, true, width, TabTime, middlePass),
			(TabTime * (middlePass ? 1.5 : 1)));
		IsEnabled = true;
		updateTabs?.Invoke();
	}

	public async void PassTab(bool back, double width) {
		double d = back ? -(width + 24) : (width + 24);
		IsEnabled = false;
		await AnimateMargin(list, new Thickness(d, 0, -d, 0), new Thickness(-d, 0, d, 0), TabTime * 1.5);
		IsEnabled = true;
	}

	private static Thickness MakeThickness(bool back, bool enter, bool isTab, double width, double duration, bool middlePass = false) {
		double d = width + 24;
		if (middlePass) d *= 2;
		if (back == enter) d = -d;
		return enter ? new Thickness(d, 0, -d, 0) : new Thickness(d, 0, -d, 0);
	}

	private static async Task AnimateMargin(Control target, Thickness fromThickness, Thickness toThickness, double duration) {
		target.Margin = fromThickness;
		using var cts = new CancellationTokenSource();
		var anim = new Animation {
			Duration = TimeSpan.FromSeconds(duration),
			FillMode = FillMode.Forward,
			Easing = new ElasticEaseInOut(),
			Children = {
				new KeyFrame {
					Cue = new Cue(0d),
					Setters = { new Setter(MarginProperty, fromThickness) }
				},
				new KeyFrame {
					Cue = new Cue(1d),
					Setters = { new Setter(MarginProperty, toThickness) }
				}
			}
		};
		await anim.RunAsync(target, cts.Token);
		target.Margin = toThickness;
	}
}
