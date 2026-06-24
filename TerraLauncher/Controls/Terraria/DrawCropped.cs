using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace TerraLauncher.Controls.Terraria;

public class CroppedFrame {
	public Bitmap? Source;
	public Rect[] SourceRects = Array.Empty<Rect>(); // 9 regions
	public int Corner;
	public int Side;
}

public static class CroppedFrames {
	public static CroppedFrame? WindowFrame;
	public static CroppedFrame? SetupFrame;
	public static CroppedFrame? SetupFrameFocused;
	public static CroppedFrame? ButtonFrame;
	public static CroppedFrame? ButtonFrameLight;
	public static CroppedFrame? ButtonFrameDark;

	private static bool _initialized;

	public static void EnsureInitialized() {
		if (_initialized) return;
		_initialized = true;
		string uri = "avares://TerraLauncher/Resources/Terraria/Controls/";
		WindowFrame = DrawCropped.GetCroppedFrame(10, 2, uri + "WindowFrame.png");
		SetupFrame = DrawCropped.GetCroppedFrame(6, 2, uri + "SetupFrame.png");
		SetupFrameFocused = DrawCropped.GetCroppedFrame(6, 2, uri + "SetupFrameFocused.png");
		ButtonFrame = DrawCropped.GetCroppedFrame(4, 2, uri + "ButtonFrame.png");
		ButtonFrameLight = DrawCropped.GetCroppedFrame(4, 2, uri + "ButtonFrameLight.png");
		ButtonFrameDark = DrawCropped.GetCroppedFrame(4, 2, uri + "ButtonFrameDark.png");
	}
}

public static class DrawCropped {
	public static void DrawFrame(DrawingContext ctx, CroppedFrame? frame, double width, double height) {
		if (frame?.Source == null) return;
		double c = frame.Corner;
		double s = frame.Side;
		if (width - c * 2 < 0 || height - c * 2 < 0) return;

		var rects = frame.SourceRects;
		// Corners
		ctx.DrawImage(frame.Source, rects[0], new Rect(0, 0, c, c));
		ctx.DrawImage(frame.Source, rects[1], new Rect(width - c, 0, c, c));
		ctx.DrawImage(frame.Source, rects[2], new Rect(0, height - c, c, c));
		ctx.DrawImage(frame.Source, rects[3], new Rect(width - c, height - c, c, c));
		// Edges
		ctx.DrawImage(frame.Source, rects[4], new Rect(c, 0, width - c * 2, c));
		ctx.DrawImage(frame.Source, rects[5], new Rect(0, c, c, height - c * 2));
		ctx.DrawImage(frame.Source, rects[6], new Rect(c, height - c, width - c * 2, c));
		ctx.DrawImage(frame.Source, rects[7], new Rect(width - c, c, c, height - c * 2));
		// Center
		ctx.DrawImage(frame.Source, rects[8], new Rect(c, c, width - c * 2, height - c * 2));
	}

	public static CroppedFrame? GetCroppedFrame(int corner, int side, string uri) {
		try {
			using var stream = AssetLoader.Open(new Uri(uri));
			var source = new Bitmap(stream);
			return GetCroppedFrame(source, corner, side);
		}
		catch {
			return null;
		}
	}

	public static CroppedFrame GetCroppedFrame(Bitmap source, int corner, int side) {
		int c = corner, s = side;
		var rects = new Rect[] {
			new(0, 0, c, c),
			new(c + s, 0, c, c),
			new(0, c + s, c, c),
			new(c + s, c + s, c, c),
			new(c, 0, s, c),
			new(0, c, c, s),
			new(c, c + s, s, c),
			new(c + s, c, c, s),
			new(c, c, s, s)
		};
		return new CroppedFrame { Source = source, SourceRects = rects, Corner = corner, Side = side };
	}
}
