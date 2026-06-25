using System.Collections.Generic;
using System.Diagnostics;

namespace TerraLauncher;

public static class ProcessTracker {
	private static readonly List<Process> _tracked = new();
	private static readonly object _lock = new();

	public static void Track(Process p) {
		lock (_lock) _tracked.Add(p);
		p.EnableRaisingEvents = true;
		p.Exited += (_, _) => { lock (_lock) _tracked.Remove(p); };
	}

	public static void KillAll() {
		lock (_lock) {
			foreach (var p in _tracked) {
				try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
				try { p.Dispose(); } catch { }
			}
			_tracked.Clear();
		}
	}
}
