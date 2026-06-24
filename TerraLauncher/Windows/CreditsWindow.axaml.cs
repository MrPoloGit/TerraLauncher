using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;

namespace TerraLauncher.Windows;

public partial class CreditsWindow : Window {
	public CreditsWindow() {
		InitializeComponent();
	}

	private void OnLinkClicked(object? sender, PointerReleasedEventArgs e) {
		if (sender is Avalonia.Controls.TextBlock tb && tb.Tag is string url)
			Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
	}

	public static new void Show(Window owner) {
		var w = new CreditsWindow { Owner = owner };
		w.ShowDialog(owner);
	}
}
