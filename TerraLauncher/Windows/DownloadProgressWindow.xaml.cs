using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TerraLauncher.Windows {
	// Modal download dialog: log output, progress bar, cancel.
	// Usage: await DownloadProgressWindow.RunAsync(owner, title, async (ui, ct) => { ... return success; });
	public partial class DownloadProgressWindow : Window {
		private readonly CancellationTokenSource cts = new CancellationTokenSource();
		private readonly StringBuilder log = new StringBuilder();
		private bool finished = false;

		public CancellationToken CancellationToken => cts.Token;

		public DownloadProgressWindow() {
			InitializeComponent();
		}

		public void SetTitle(string title) =>
			Dispatcher.BeginInvoke(new Action(() => labelTitle.Text = title));

		public void AppendLog(string line) {
			Dispatcher.BeginInvoke(new Action(() => {
				log.AppendLine(line);
				// Keep the log bounded so very chatty tools don't grow this unbounded.
				if (log.Length > 60000)
					log.Remove(0, log.Length - 40000);
				logText.Text = log.ToString();
				logScroll.ScrollToEnd();
			}));
		}

		// fraction: 0..1, or negative to hide/reset the bar.
		public void SetProgress(double fraction) {
			Dispatcher.BeginInvoke(new Action(() => {
				if (fraction < 0) { progressFill.Width = 0; return; }
				double w = progressTrack.ActualWidth - 2;
				progressFill.Width = Math.Max(0, Math.Min(1, fraction) * Math.Max(0, w));
			}));
		}

		private void OnCancelClicked(object sender, RoutedEventArgs e) {
			if (finished) { Close(); return; }
			AppendLog("Cancelling...");
			cts.Cancel();
		}

		private void OnWindowClosing(object sender, CancelEventArgs e) {
			if (!finished) {
				e.Cancel = true;
				cts.Cancel();
			}
		}

		// Runs `work` while the dialog is shown. Returns work's result (false on
		// cancel/failure). The dialog stays open on failure so the user can read
		// the log; it closes itself automatically on success.
		public static async Task<bool> RunAsync(Window owner, string title,
			Func<DownloadProgressWindow, CancellationToken, Task<bool>> work) {
			DownloadProgressWindow w = new DownloadProgressWindow();
			w.Owner = owner;
			w.labelTitle.Text = title;
			w.Title = title;

			bool success = false;
			w.ContentRendered += async (s, e) => {
				try {
					success = await work(w, w.cts.Token);
				}
				catch (OperationCanceledException) {
					w.AppendLog("Cancelled.");
				}
				catch (Exception ex) {
					w.AppendLog("ERROR: " + ex.Message);
				}
				w.finished = true;
				if (success) {
					w.Close();
				}
				else {
					w.SetProgress(-1);
					// Already on the UI thread here (this is a ContentRendered
					// continuation), so no Dispatcher marshaling needed.
					w.buttonCancel.Content = "Close";
				}
			};

			w.ShowDialog();
			return success;
		}
	}
}
