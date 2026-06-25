using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using TerraLauncher.Instances;

namespace TerraLauncher.Windows;

public partial class AddInstanceWindow : Window {
	private bool _closing;

	public AddInstanceWindow() {
		InitializeComponent();
		Opened  += async (_, _) => { Sounds.PlayOpen(); await FadeAsync(0, 1, 0.3); };
		Closing += OnWindowClosing;
	}

	private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e) {
		if (_closing) return;
		_closing = true;
		e.Cancel = true;
		FadeAndClose();
	}

	private async void FadeAndClose() {
		await FadeAsync(1, 0, 0.3);
		Closing -= OnWindowClosing;
		Close();
	}

	private async Task FadeAsync(double from, double to, double seconds) {
		Opacity = from;
		var anim = new Animation {
			Duration = TimeSpan.FromSeconds(seconds),
			FillMode = FillMode.Forward,
			Children = {
				new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(OpacityProperty, from) } },
				new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(OpacityProperty, to) } }
			}
		};
		using var cts = new CancellationTokenSource();
		await anim.RunAsync(this, cts.Token);
		Opacity = to;
	}

	// ── Category tile handlers ─────────────────────────────────────────

	private async void OnTerraria(object? sender, RoutedEventArgs e)  => await OpenPicker(InstanceCategory.Terraria);
	private async void OnTModLoader(object? sender, RoutedEventArgs e) => await OpenPicker(InstanceCategory.TModLoader);
	private async void OnTAPI(object? sender, RoutedEventArgs e)       => await OpenPicker(InstanceCategory.TAPI);
	private async void OnTConfig(object? sender, RoutedEventArgs e)    => await OpenPicker(InstanceCategory.TConfig);
	private async void OnStandAlone(object? sender, RoutedEventArgs e) => await OpenPicker(InstanceCategory.StandAlone);

	private async Task OpenPicker(InstanceCategory category) {
		var picker = new VersionPickerWindow(category);
		bool downloaded = await picker.ShowDialog<bool>(this);
		if (downloaded) {
			_closing = true;
			Closing -= OnWindowClosing;
			await FadeAsync(1, 0, 0.25);
			Close();
		}
	}

	public static async Task ShowDialogAsync(Window owner) {
		var w = new AddInstanceWindow();
		await w.ShowDialog(owner);
	}
}
