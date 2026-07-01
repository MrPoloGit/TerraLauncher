using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TerraLauncher.Util;

// Sets the macOS Dock icon at runtime. Needed when running outside a .app
// bundle (e.g. `dotnet run`), where Info.plist/icns don't apply.
internal static class MacDockIcon {
	[DllImport("/usr/lib/libobjc.dylib")]
	private static extern IntPtr objc_getClass(string name);

	[DllImport("/usr/lib/libobjc.dylib")]
	private static extern IntPtr sel_registerName(string name);

	[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
	private static extern IntPtr Send(IntPtr receiver, IntPtr selector);

	[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
	private static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr arg);

	[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
	private static extern IntPtr Send(IntPtr receiver, IntPtr selector, IntPtr bytes, nuint length);

	public static void Set(Stream pngStream) {
		if (!OperatingSystem.IsMacOS()) return;
		try {
			using var ms = new MemoryStream();
			pngStream.CopyTo(ms);
			byte[] png = ms.ToArray();

			var handle = GCHandle.Alloc(png, GCHandleType.Pinned);
			try {
				IntPtr nsData = Send(objc_getClass("NSData"),
					sel_registerName("dataWithBytes:length:"),
					handle.AddrOfPinnedObject(), (nuint)png.Length);
				if (nsData == IntPtr.Zero) return;

				IntPtr nsImage = Send(objc_getClass("NSImage"), sel_registerName("alloc"));
				nsImage = Send(nsImage, sel_registerName("initWithData:"), nsData);
				if (nsImage == IntPtr.Zero) return;

				IntPtr nsApp = Send(objc_getClass("NSApplication"),
					sel_registerName("sharedApplication"));
				Send(nsApp, sel_registerName("setApplicationIconImage:"), nsImage);
			}
			finally {
				handle.Free();
			}
		}
		catch { }
	}
}
