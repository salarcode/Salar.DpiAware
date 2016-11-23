using System;
using System.Runtime.InteropServices;

namespace Salar.DpiAware
{
	class Win32Api
	{
		[DllImport("kernel32.dll")]
		internal static extern IntPtr LoadLibrary(string dllToLoad);

		[DllImport("kernel32.dll")]
		internal static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

		[DllImport("kernel32.dll")]
		internal static extern bool FreeLibrary(IntPtr hModule);

		// Get handle to monitor that has the largest intersection with a specified window.
		[DllImport("User32.dll", SetLastError = true)]
		internal static extern IntPtr MonitorFromWindow(IntPtr hwnd,
														int dwFlags);

		// Get handle to monitor that has the largest intersection with a specified rectangle.
		[DllImport("User32.dll", SetLastError = true)]
		internal static extern IntPtr MonitorFromRect([In] ref RECT lprc,
													  int dwFlags);

		// Get handle to monitor that contains a specified point.
		[DllImport("User32.dll", SetLastError = true)]
		internal static extern IntPtr MonitorFromPoint(POINT pt, int dwFlags);

		internal const int MONITORINFOF_PRIMARY = 0x00000001;
		internal const int MONITOR_DEFAULTTONEAREST = 0x00000002;
		internal const int MONITOR_DEFAULTTONULL = 0x00000000;
		internal const int MONITOR_DEFAULTTOPRIMARY = 0x00000001;

		[StructLayout(LayoutKind.Sequential)]
		internal struct POINT
		{
			internal int x;
			internal int y;

			internal POINT(int x, int y)
			{
				this.x = x;
				this.y = y;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct RECT
		{
			internal int left;
			internal int top;
			internal int right;
			internal int bottom;
		}

		// Get DPI from handle to a specified monitor (Windows 8.1 or newer is required).
		[DllImport("Shcore.dll", SetLastError = true)]
		internal static extern int GetDpiForMonitor(IntPtr hmonitor,
													Monitor_DPI_Type dpiType,
													out uint dpiX,
													out uint dpiY);

		internal enum Monitor_DPI_Type : int
		{
			MDT_Effective_DPI = 0,
			MDT_Angular_DPI = 1,
			MDT_Raw_DPI = 2,
			MDT_Default = MDT_Effective_DPI
		}

		// Equivalent to LOWORD macro
		internal static short GetLoWord(int dword)
		{
			return (short)(dword & 0xffff);
		}

		// Get device-specific information.
		[DllImport("Gdi32.dll", SetLastError = true)]
		internal static extern int GetDeviceCaps(IntPtr hdc,
												 int nIndex);

		internal const int LOGPIXELSX = 88;

		[DllImport("User32.dll", SetLastError = true)]
		internal static extern IntPtr GetDC(IntPtr hWnd);

		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

		[return: MarshalAs(UnmanagedType.Bool)]
		private delegate bool EnableNonClientDpiScaling(IntPtr hWnd);

		/// <summary>
		/// In high-DPI displays, enables automatic display scaling of the non-client area portions of the specified top-level window. Must be called during the initialization of that window.
		/// </summary>
		/// <param name="hWnd">The window that should have automatic scaling enabled.</param>
		/// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error information, call GetLastError.</returns>
		internal static bool CallEnableNonClientDpiScaling(IntPtr hWnd)
		{
			var user32 = LoadLibrary("User32.dll");
			if (user32 == IntPtr.Zero)
				return false;
			try
			{
				var dpiScalling = GetProcAddress(user32, "EnableNonClientDpiScaling");

				if (dpiScalling == IntPtr.Zero)
					return false;

				var enableNonClientDpiScaling = (EnableNonClientDpiScaling)
					Marshal.GetDelegateForFunctionPointer(dpiScalling, typeof(EnableNonClientDpiScaling));

				return enableNonClientDpiScaling(hWnd);
			}
			finally
			{
				FreeLibrary(user32);
			}
		}
	}

}
