using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TerraLauncher;

public static class Sounds {
	private static byte[]? _tickBytes;
	private static byte[]? _openBytes;
	private static byte[]? _closeBytes;

	static Sounds() {
		_tickBytes  = LoadEmbedded("TerraLauncher.Resources.Terraria.Sounds.MenuTick.wav");
		_openBytes  = LoadEmbedded("TerraLauncher.Resources.Terraria.Sounds.MenuOpen.wav");
		_closeBytes = LoadEmbedded("TerraLauncher.Resources.Terraria.Sounds.MenuClose.wav");
	}

	private static byte[]? LoadEmbedded(string name) {
		try {
			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
			if (stream == null) return null;
			var bytes = new byte[stream.Length];
			_ = stream.Read(bytes, 0, bytes.Length);
			return bytes;
		}
		catch { return null; }
	}

	private static void Play(byte[]? data) {
		if (Config.Muted || data == null) return;
		// Audio via external process (afplay/aplay) causes system-wide stutter on macOS/Linux.
		// Restrict to Windows until a cross-platform managed audio library is added.
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
		try {
			PlayWindows(data);
		}
		catch { }
	}

	private static void PlayWindows(byte[] data) {
		try {
			var playerType = Type.GetType("System.Media.SoundPlayer, System.Windows.Extensions");
			if (playerType == null) return;
			using var ms = new MemoryStream(data);
			var ctor   = playerType.GetConstructor(new[] { typeof(Stream) });
			var player = ctor?.Invoke(new object[] { ms });
			playerType.GetMethod("Play")?.Invoke(player, null);
			(player as IDisposable)?.Dispose();
		}
		catch { }
	}

	private static void PlayLinux(byte[] data) {
		if (!PlayViaTempFile(data, "aplay", "-q"))
			PlayViaTempFile(data, "paplay", null);
	}

	private static bool PlayViaTempFile(byte[] data, string exe, string? args) {
		string tmp = Path.ChangeExtension(Path.GetTempFileName(), ".wav");
		try {
			File.WriteAllBytes(tmp, data);
			string fullArgs = args != null ? $"{args} \"{tmp}\"" : $"\"{tmp}\"";
			var psi = new ProcessStartInfo(exe, fullArgs) {
				UseShellExecute        = false,
				CreateNoWindow         = true,
				RedirectStandardOutput = false,
				RedirectStandardError  = false,
			};
			var p = Process.Start(psi);
			if (p == null) { TryDelete(tmp); return false; }
			p.EnableRaisingEvents = true;
			p.Exited += (_, _) => { p.Dispose(); TryDelete(tmp); };
			return true;
		}
		catch { TryDelete(tmp); return false; }
	}

	private static void TryDelete(string path) {
		try { File.Delete(path); } catch { }
	}

	public static void PlayTick()  => Play(_tickBytes);
	public static void PlayOpen()  => Play(_openBytes);
	public static void PlayClose() => Play(_closeBytes);
}
