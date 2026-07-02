using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;
using TerraLauncher.Setups;

namespace TerraLauncher.Controls.Terraria;

public partial class TerrariaSetupList : UserControl {
	private const double FolderTime = 0.4;
	private const double TabTime = 0.3;

	private Action<SetupFolder>? _navigateForward;
	private Action? _navigateBack;
	private SetupFolder? _folder;

	public SetupFolder? Folder => _folder;

	public TerrariaSetupList() {
		InitializeComponent();
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
		if (list.Children.Count == 0)
			ShowEmptyMessage("No instances yet.\nClick + below to add one.");
	}

	// Flat list of entries only — used by search/filter results
	public void PopulateFlat(System.Collections.Generic.IEnumerable<Setup> setups, string emptyMessage) {
		list.Children.Clear();
		foreach (var setup in setups)
			list.Children.Add(new TerrariaSetupEntry(setup));
		if (list.Children.Count == 0)
			ShowEmptyMessage(emptyMessage);
	}

	private void ShowEmptyMessage(string message) {
		list.Children.Add(new TextBlock {
			Text = message,
			FontSize = 22,
			Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#9999BB")),
			TextWrapping = Avalonia.Media.TextWrapping.Wrap,
			TextAlignment = Avalonia.Media.TextAlignment.Center,
			HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
			Margin = new Thickness(20, 60, 20, 0),
		});
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
