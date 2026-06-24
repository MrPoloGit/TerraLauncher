using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TerraLauncher.Util;

public static class Extensions {
	public static string TrimWhitespace(this string s) => s.Trim(' ', '\t', '\r', '\n');

	public static void Fill<T>(this T[] array, T with) {
		for (int i = 0; i < array.Length; i++) array[i] = with;
	}

	public static void Swap<T>(this T[] array, int indexA, int indexB) {
		(array[indexA], array[indexB]) = (array[indexB], array[indexA]);
	}

	public static void Move<T>(this List<T> list, int oldIndex, int newIndex) {
		T t = list[oldIndex];
		list.RemoveAt(oldIndex);
		list.Insert(newIndex, t);
	}

	public static void Swap<T>(this List<T> list, int indexA, int indexB) {
		(list[indexA], list[indexB]) = (list[indexB], list[indexA]);
	}

	public static bool IsEmpty<T>(this ICollection<T> collection) => collection.Count == 0;

	public static void Show(this Process process) {
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			ShowWindowNative(process.MainWindowHandle, 9);
	}

	public static void Hide(this Process process) {
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			ShowWindowNative(process.MainWindowHandle, 0);
	}

	[DllImport("user32.dll", EntryPoint = "ShowWindow")]
	private static extern bool ShowWindowNative(IntPtr hWnd, int nCmdShow);

	public static TEnum SetFlag<TEnum>(this Enum enumValue, TEnum flag, bool set = true)
		where TEnum : struct, IComparable, IFormattable, IConvertible {
		var underlyingType = Enum.GetUnderlyingType(enumValue.GetType());
		dynamic valueAsInt = Convert.ChangeType(enumValue, underlyingType);
		dynamic flagAsInt = Convert.ChangeType(flag, underlyingType);
		if (set) valueAsInt |= flagAsInt;
		else valueAsInt &= ~flagAsInt;
		return (TEnum)valueAsInt;
	}
}
