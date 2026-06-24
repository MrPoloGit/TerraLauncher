using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TerraLauncher.Windows;

public enum MessageIcon { Info, Question, Warning, Error }
public enum MsgBoxButton { OK, OKCancel, YesNo, YesNoCancel }
public enum MsgBoxResult { None, OK, Cancel, Yes, No }

public partial class TriggerMessageBox : Window {
	private MsgBoxResult _result = MsgBoxResult.None;

	public TriggerMessageBox(MessageIcon icon, string title, string message, MsgBoxButton buttons,
		string? b1 = null, string? b2 = null, string? b3 = null) {
		InitializeComponent();
		Title = title;
		textBlockMessage.Text = message;

		switch (buttons) {
		case MsgBoxButton.OK:
			SetupBtn(button1, b1 ?? "OK", MsgBoxResult.OK, isDefault: true);
			button2.IsVisible = false;
			button3.IsVisible = false;
			_result = MsgBoxResult.OK;
			break;
		case MsgBoxButton.OKCancel:
			SetupBtn(button1, b1 ?? "OK", MsgBoxResult.OK, isDefault: true);
			SetupBtn(button2, b2 ?? "Cancel", MsgBoxResult.Cancel, isCancel: true);
			button3.IsVisible = false;
			_result = MsgBoxResult.Cancel;
			break;
		case MsgBoxButton.YesNo:
			SetupBtn(button1, b1 ?? "Yes", MsgBoxResult.Yes, isDefault: true);
			SetupBtn(button2, b2 ?? "No", MsgBoxResult.No, isCancel: true);
			button3.IsVisible = false;
			_result = MsgBoxResult.No;
			break;
		case MsgBoxButton.YesNoCancel:
			SetupBtn(button1, b1 ?? "Yes", MsgBoxResult.Yes, isDefault: true);
			SetupBtn(button2, b2 ?? "No", MsgBoxResult.No);
			SetupBtn(button3, b3 ?? "Cancel", MsgBoxResult.Cancel, isCancel: true);
			_result = MsgBoxResult.Cancel;
			break;
		}
	}

	private static void SetupBtn(Button btn, string label, MsgBoxResult tag, bool isDefault = false, bool isCancel = false) {
		btn.Content = label;
		btn.Tag = tag;
		if (isDefault) btn.IsDefault = true;
		if (isCancel) btn.IsCancel = true;
	}

	private void OnButtonClicked(object? sender, RoutedEventArgs e) {
		if (sender is Button btn && btn.Tag is MsgBoxResult r)
			_result = r;
		Close(_result);
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
