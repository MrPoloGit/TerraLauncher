using System.Threading.Tasks;
using System.Windows;

namespace TerraLauncher.Windows {
	// Generic 1-2 field input dialog (Steam login, Steam Guard code, etc.)
	public partial class TextPromptWindow : Window {
		private bool ok = false;

		public TextPromptWindow() {
			InitializeComponent();
			Loaded += (s, e) => textBox1.Focus();
		}

		private void OnOK(object sender, RoutedEventArgs e) {
			ok = true;
			Close();
		}

		// Single-field prompt. Returns null if cancelled.
		public static string ShowAsync(Window owner, string title, string message, string fieldLabel) {
			TextPromptWindow w = new TextPromptWindow();
			w.Owner = owner;
			w.Title = title;
			w.textBlockMessage.Text = message;
			w.labelField1.Content = fieldLabel;
			w.panelField2.Visibility = Visibility.Collapsed;
			w.clientArea.Height -= 96;

			w.ShowDialog();
			return w.ok ? w.textBox1.Text : null;
		}

		// Two-field prompt (second field masked). Returns null if cancelled.
		public static (string User, string Pass)? ShowLoginAsync(Window owner, string title,
			string message, string label1, string label2) {
			TextPromptWindow w = new TextPromptWindow();
			w.Owner = owner;
			w.Title = title;
			w.textBlockMessage.Text = message;
			w.labelField1.Content = label1;
			w.labelField2.Content = label2;

			w.ShowDialog();
			if (!w.ok) return null;
			return (w.textBox1.Text ?? "", w.passwordBox2.Password ?? "");
		}
	}
}
