using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace TerraLauncher.Windows;

public partial class ErrorMessageBox : Window {
	private readonly Exception? _exception;
	private readonly object? _exceptionObject;
	private bool _viewingFull;
	private System.Timers.Timer? _copyTimer;

	public ErrorMessageBox(Exception exception, bool alwaysContinue) {
		InitializeComponent();
		_exception = exception;
		textBlockMessage.Text = "Exception:\n" + exception.Message;
		SetupCommon(alwaysContinue, hasException: true);
	}

	public ErrorMessageBox(object exceptionObject, bool alwaysContinue) {
		InitializeComponent();
		if (exceptionObject is Exception ex) {
			_exception = ex;
		}
		else {
			_exceptionObject = exceptionObject;
			buttonException.IsEnabled = false;
		}
		textBlockMessage.Text = "Exception:\n" + (exceptionObject is Exception e2 ? e2.Message : exceptionObject.ToString());
		SetupCommon(alwaysContinue, hasException: exceptionObject is Exception);
	}

	private void SetupCommon(bool alwaysContinue, bool hasException) {
		if (alwaysContinue) {
			buttonExit.IsVisible = false;
			buttonContinue.IsDefault = true;
		}
		_copyTimer = new System.Timers.Timer(1000) { AutoReset = false };
		_copyTimer.Elapsed += (_, _) => Dispatcher.UIThread.Post(() => buttonCopy.Content = "Copy to Clipboard");
	}

	private void OnExit(object? sender, RoutedEventArgs e) => Close(true);

	private async void OnCopyToClipboard(object? sender, RoutedEventArgs e) {
		string text = _exception?.ToString() ?? _exceptionObject?.ToString() ?? "";
		await TopLevel.GetTopLevel(this)!.Clipboard!.SetTextAsync(text);
		buttonCopy.Content = "Exception Copied!";
		_copyTimer?.Stop();
		_copyTimer?.Start();
	}

	private void OnSeeFullException(object? sender, RoutedEventArgs e) {
		_viewingFull = !_viewingFull;
		if (_viewingFull) {
			buttonException.Content = "Hide Full Exception";
			textBlockMessage.Text = "Exception:\n" + _exception?.ToString();
		}
		else {
			buttonException.Content = "See Full Exception";
			textBlockMessage.Text = "Exception:\n" + _exception?.Message;
		}
		scrollViewer.ScrollToHome();
	}

	public static async Task<bool> ShowAsync(Exception exception, bool alwaysContinue = false) {
		var box = new ErrorMessageBox(exception, alwaysContinue);
		return await box.ShowDialog<bool>(null!);
	}
	public static async Task<bool> ShowAsync(object exceptionObject, bool alwaysContinue = false) {
		var box = new ErrorMessageBox(exceptionObject, alwaysContinue);
		return await box.ShowDialog<bool>(null!);
	}
}
