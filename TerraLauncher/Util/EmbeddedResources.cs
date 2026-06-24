using System.IO;
using System.Reflection;

namespace TerraLauncher.Util;

public static class EmbeddedResources {
	public static void Extract(string resourcePath, byte[] resourceBytes) {
		string? dirName = Path.GetDirectoryName(resourcePath);
		if (dirName != null && !Directory.Exists(dirName))
			Directory.CreateDirectory(dirName);

		bool rewrite = true;
		if (File.Exists(resourcePath)) {
			byte[] existing = File.ReadAllBytes(resourcePath);
			if (resourceBytes.Length == existing.Length) {
				rewrite = false;
				for (int i = 0; i < existing.Length; i++) {
					if (existing[i] != resourceBytes[i]) { rewrite = true; break; }
				}
			}
		}
		if (rewrite)
			File.WriteAllBytes(resourcePath, resourceBytes);
	}

	public static void Extract(string resourcePath, Stream stream) {
		using var ms = new MemoryStream();
		stream.CopyTo(ms);
		Extract(resourcePath, ms.ToArray());
	}

	public static void Extract(string resourcePath, string resourceName) {
		using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)!;
		Extract(resourcePath, stream);
	}
}
