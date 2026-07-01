using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace TerraLauncher.Windows;

// Generic 1–2 field input dialog (Steam login, Steam Guard code, etc.)
public partial class TextPromptWindow : Window {
	private bool _closing;
	private (string?, string?)? _result;

	public TextPromptWindow() {
		InitializeComponent();
		Opened  += async (_, _) => { Sounds.PlayOpen(); await FadeAsync(0, 1, 0.2); textBox1.Focus(); };
		Closing += OnWindowClosing;
	}

	private void OnOK(object? sender, RoutedEventArgs e) {
		_result = (textBox1.Text, panelField2.IsVisible ? textBox2.Text : null);
		FadeAndClose();
	}

	private void OnCancel(object? sender, RoutedEventArgs e) => FadeAndClose();

	private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e) {
		if (_closing) return;
		_closing = true;
		e.Cancel = true;
		FadeAndClose();
	}

	private async void FadeAndClose() {
		_closing = true;
		Closing -= OnWindowClosing;
		Sounds.PlayClose();
		await FadeAsync(1, 0, 0.15);
		Close(_result);
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

	// Single-field prompt. Returns null if cancelled.
	public static async Task<string?> ShowAsync(Window owner, string title, string message,
		string fieldLabel, bool password = false) {
		var w = new TextPromptWindow { Title = title };
		w.textBlockMessage.Text = message;
		w.labelField1.Text = fieldLabel;
		if (password) w.textBox1.PasswordChar = '●';
		w.panelField2.IsVisible = false;
		var result = await w.ShowDialog<(string?, string?)?>(owner);
		return result?.Item1;
	}

	// Two-field prompt (second field masked). Returns null if cancelled.
	public static async Task<(string User, string Pass)?> ShowLoginAsync(Window owner, string title,
		string message, string label1, string label2) {
		var w = new TextPromptWindow { Title = title };
		w.textBlockMessage.Text = message;
		w.labelField1.Text = label1;
		w.labelField2.Text = label2;
		var result = await w.ShowDialog<(string?, string?)?>(owner);
		if (result == null) return null;
		return (result.Value.Item1 ?? "", result.Value.Item2 ?? "");
	}
}
