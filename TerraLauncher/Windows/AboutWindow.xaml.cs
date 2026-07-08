using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TerraLauncher.Windows {
	/**<summary>The window showing information about the program.</summary>*/
	public partial class AboutWindow : Window {
		//========= CONSTRUCTORS =========
		#region Constructors

		/**<summary>Constructs the about window.</summary>*/
		public AboutWindow() {
			InitializeComponent();

			DateTime? buildDate = GetLinkerTime();
			this.labelVersion.Content = Assembly.GetExecutingAssembly().GetName().Version.ToString() + " Release";
			this.labelBuildDate.Content = buildDate.HasValue
				? buildDate.Value.ToShortDateString() + " (" + buildDate.Value.ToShortTimeString() + ")"
				: "Unknown";
		}

		#endregion
		//=========== HELPERS ============
		#region Helpers

		/**<summary>Gets the build date of the program, or null if it can't be determined.</summary>*/
		private DateTime? GetLinkerTime(TimeZoneInfo target = null) {
			// Assembly.Location is empty for single-file published apps - the
			// assembly is bundled inside the exe rather than existing as its own
			// file - but Environment.ProcessPath still points at a real PE file
			// on disk in that case, so prefer it.
			string filePath = Environment.ProcessPath;
			if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
				filePath = Assembly.GetExecutingAssembly().Location;
			if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
				return null;

			const int c_PeHeaderOffset = 60;
			const int c_LinkerTimestampOffset = 8;

			try {
				var buffer = new byte[2048];

				using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
					stream.ReadExactly(buffer, 0, 2048);

				var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
				var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
				var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

				var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

				var tz = target ?? TimeZoneInfo.Local;
				return TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);
			}
			catch {
				return null;
			}
		}

		#endregion
		//============ EVENTS ============
		#region Events
		
		private void OnWindowLoaded(object sender, RoutedEventArgs e) {
			clientArea.Height = 214 + textBlockDescription.ActualHeight;
		}

		#endregion
		//=========== SHOWING ============
		#region Showing

		/**<summary>Shows the window.</summary>*/
		public static void Show(Window owner) {
			AboutWindow window = new AboutWindow();
			window.Owner = owner;
			window.ShowDialog();
		}

		#endregion
	}
}
