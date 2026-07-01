using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;

namespace TerraLauncher.Windows;

// Modal download dialog: log output, progress bar, cancel.
// Usage: await DownloadProgressWindow.RunAsync(owner, title, async (ui, ct) => { ... return success; });
public partial class DownloadProgressWindow : Window {
	private readonly CancellationTokenSource _cts = new();
	private readonly StringBuilder _log = new();
	private bool _finished;
	private bool _closing;

	public CancellationToken CancellationToken => _cts.Token;

	public DownloadProgressWindow() {
		InitializeComponent();
		Opened  += async (_, _) => { Sounds.PlayOpen(); await FadeAsync(0, 1, 0.2); };
		Closing += OnWindowClosing;
	}

	// ── Thread-safe UI updates ─────────────────────────────────────────

	public void SetTitle(string title) =>
		Dispatcher.UIThread.Post(() => labelTitle.Text = title);

	public void AppendLog(string line) {
		Dispatcher.UIThread.Post(() => {
			_log.AppendLine(line);
			// Keep the log bounded
			if (_log.Length > 60_000) _log.Remove(0, _log.Length - 40_000);
			logText.Text = _log.ToString();
			logScroll.ScrollToEnd();
		});
	}

	// fraction: 0..1, or negative to hide the bar
	public void SetProgress(double fraction) {
		Dispatcher.UIThread.Post(() => {
			if (fraction < 0) { progressFill.Width = 0; return; }
			double w = progressTrack.Bounds.Width - 2;
			progressFill.Width = Math.Max(0, Math.Min(1, fraction) * Math.Max(0, w));
		});
	}

	// ── Lifecycle ──────────────────────────────────────────────────────

	private void OnCancel(object? sender, RoutedEventArgs e) {
		if (_finished) { FadeAndClose(); return; }
		AppendLog("Cancelling…");
		_cts.Cancel();
	}

	private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e) {
		if (_closing) return;
		e.Cancel = true;
		if (!_finished) { _cts.Cancel(); return; }
		FadeAndClose();
	}

	private async void FadeAndClose() {
		if (_closing) return;
		_closing = true;
		Closing -= OnWindowClosing;
		Sounds.PlayClose();
		await FadeAsync(1, 0, 0.2);
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

	// Runs `work` while the dialog is shown. Returns work's result
	// (false on cancel/failure). The dialog stays open on failure so the
	// user can read the log; it closes itself on success.
	public static async Task<bool> RunAsync(Window owner, string title,
		Func<DownloadProgressWindow, CancellationToken, Task<bool>> work) {
		var w = new DownloadProgressWindow();
		w.labelTitle.Text = title;
		w.Title = title;

		bool success = false;
		w.Opened += async (_, _) => {
			try {
				success = await work(w, w._cts.Token);
			}
			catch (OperationCanceledException) {
				w.AppendLog("Cancelled.");
			}
			catch (Exception ex) {
				w.AppendLog("ERROR: " + ex.Message);
			}
			w._finished = true;
			if (success) {
				w.FadeAndClose();
			}
			else {
				w.SetProgress(-1);
				Dispatcher.UIThread.Post(() => w.labelCancel.Text = "Close");
			}
		};

		await w.ShowDialog(owner);
		return success;
	}
}
