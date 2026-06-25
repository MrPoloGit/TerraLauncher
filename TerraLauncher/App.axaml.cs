using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TerraLauncher.Windows;

namespace TerraLauncher;

public partial class App : Application {
	public override void Initialize() {
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted() {
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			var mainWindow = new MainWindow();
			desktop.MainWindow = mainWindow;
			desktop.ShutdownRequested += (_, _) => {
				Config.SaveConfig();
				ProcessTracker.KillAll();
			};
		}
		AppDomain.CurrentDomain.ProcessExit += (_, _) => ProcessTracker.KillAll();
		base.OnFrameworkInitializationCompleted();
	}
}
