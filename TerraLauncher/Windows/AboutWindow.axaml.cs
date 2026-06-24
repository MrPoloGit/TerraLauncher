using System;
using System.Reflection;
using Avalonia.Controls;

namespace TerraLauncher.Windows;

public partial class AboutWindow : Window {
	public AboutWindow() {
		InitializeComponent();
		var asm = Assembly.GetExecutingAssembly();
		labelVersion.Text = (asm.GetName().Version?.ToString() ?? "1.0") + " Release";
		labelBuildDate.Text = DateTime.Now.ToShortDateString();
	}

	public static new void Show(Window owner) {
		var w = new AboutWindow { Owner = owner };
		w.ShowDialog(owner);
	}
}
