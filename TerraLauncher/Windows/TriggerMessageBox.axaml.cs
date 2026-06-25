using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;

namespace TerraLauncher.Windows;

public enum MessageIcon { Info, Question, Warning, Error }
public enum MsgBoxButton { OK, OKCancel, YesNo, YesNoCancel }
public enum MsgBoxResult { None, OK, Cancel, Yes, No }

public partial class TriggerMessageBox : Window {
	private MsgBoxResult _result = MsgBoxResult.None;
	private bool _closing;

	public TriggerMessageBox(MessageIcon icon, string title, string message, MsgBoxButton buttons,
		string? b1 = null, string? b2 = null, string? b3 = null) {
		InitializeComponent();
		Title = title;
		textBlockMessage.Text = message;

		(textBlockIcon.Text, textBlockIcon.Foreground) = icon switch {
			MessageIcon.Warning  => ("⚠", new SolidColorBrush(Color.Parse("#FFAA44"))),
			MessageIcon.Error    => ("✖", new SolidColorBrush(Color.Parse("#FF5555"))),
			MessageIcon.Question => ("?", new SolidColorBrush(Color.Parse("#88AAFF"))),
			_                    => ("ℹ", new SolidColorBrush(Color.Parse("#88AAFF"))),
		};

		switch (buttons) {
		case MsgBoxButton.OK:
			SetupBtn(button1, label1, b1 ?? "OK",     MsgBoxResult.OK,     isDefault: true);
			button2.IsVisible = false;
			button3.IsVisible = false;
			_result = MsgBoxResult.OK;
			break;
		case MsgBoxButton.OKCancel:
			SetupBtn(button1, label1, b1 ?? "OK",     MsgBoxResult.OK,     isDefault: true);
			SetupBtn(button2, label2, b2 ?? "Cancel", MsgBoxResult.Cancel, isCancel: true);
			button3.IsVisible = false;
			_result = MsgBoxResult.Cancel;
			break;
		case MsgBoxButton.YesNo:
			SetupBtn(button1, label1, b1 ?? "Yes",    MsgBoxResult.Yes,    isDefault: true);
			SetupBtn(button2, label2, b2 ?? "No",     MsgBoxResult.No,     isCancel: true);
			button3.IsVisible = false;
			_result = MsgBoxResult.No;
			break;
		case MsgBoxButton.YesNoCancel:
			SetupBtn(button1, label1, b1 ?? "Yes",    MsgBoxResult.Yes,    isDefault: true);
			SetupBtn(button2, label2, b2 ?? "No",     MsgBoxResult.No);
			SetupBtn(button3, label3, b3 ?? "Cancel", MsgBoxResult.Cancel, isCancel: true);
			_result = MsgBoxResult.Cancel;
			break;
		}

		Opened  += async (_, _) => { Sounds.PlayOpen(); await FadeAsync(0, 1, 0.25); };
		Closing += OnWindowClosing;
	}

	private static void SetupBtn(Button btn, TextBlock lbl, string label, MsgBoxResult result,
		bool isDefault = false, bool isCancel = false) {
		lbl.Text  = label;
		btn.Tag   = result;
		if (isDefault) btn.IsDefault = true;
		if (isCancel)  btn.IsCancel  = true;
	}

	private void OnButtonClicked(object? sender, RoutedEventArgs e) {
		if (_closing) return;
		if (sender is Control c && c.Tag is MsgBoxResult r) _result = r;
		FadeAndClose(_result);
	}

	private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e) {
		if (_closing) return;
		_closing = true;
		e.Cancel = true;
		FadeAndClose(_result);
	}

	private async void FadeAndClose(MsgBoxResult result) {
		_closing = true;
		Closing -= OnWindowClosing;
		Sounds.PlayClose();
		await FadeAsync(1, 0, 0.2);
		Close(result);
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

	public static async Task<MsgBoxResult> ShowAsync(Window? owner, MessageIcon icon, string message,
		string title = "", MsgBoxButton buttons = MsgBoxButton.OK,
		string? b1 = null, string? b2 = null, string? b3 = null) {
		var box = new TriggerMessageBox(icon, title, message, buttons, b1, b2, b3);
		if (owner != null)
			return await box.ShowDialog<MsgBoxResult>(owner);
		box.WindowStartupLocation = WindowStartupLocation.CenterScreen;
		box.Show();
		return box._result;
	}
}
