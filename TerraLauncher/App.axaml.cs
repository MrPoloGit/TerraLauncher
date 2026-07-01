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
			// Must run after AppKit finishes launching — the Dock tile is
			// created when the run loop starts, wiping icons set earlier.
			if (OperatingSystem.IsMacOS()) {
				mainWindow.Opened += (_, _) => {
					try {
						using var icon = Avalonia.Platform.AssetLoader.Open(
							new Uri("avares://TerraLauncher/Resources/Icons/AppIcon.png"));
						Util.MacDockIcon.Set(icon);
					}
					catch { }
				};
			}
			desktop.ShutdownRequested += (_, _) => {
				Config.SaveConfig();
				ProcessTracker.KillAll();
			};
		}
		AppDomain.CurrentDomain.ProcessExit += (_, _) => ProcessTracker.KillAll();
		base.OnFrameworkInitializationCompleted();
	}
}
