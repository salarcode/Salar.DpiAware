using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Salar.DpiAware
{
	/// <summary>
	/// Per-Monitor DPI Aware Form
	/// </summary>
	/// <authors>
	/// emoacht, Salar Khalilzadeh
	/// </authors>
	/// <url>
	/// https://emoacht.wordpress.com/2013/10/30/per-monitor-dpi-aware-in-windows-forms/
	/// Fixed and improved by Salar Khalilzadeh
	/// </url>
	public class HDpiForm : Form
	{
		//// DPI at design time
		//private const float dpiAtDesign = 96F;

		// Old (previous) DPI
		private float _oldDpiValue = 0;

		// New (current) DPI
		private float _newDpiValue = 0;

		//// Flag to set whether this window is being moved by user
		//private bool isBeingMoved = false;

		// Flag to set whether this window will be adjusted later
		private bool _willBeAdjusted = false;

		private bool _designMode = false;

		public delegate void OnDpiChangeEvent(HDpiForm sender, Font newFont, float newDpi, float changeFactor);

		/// <summary>
		/// Occurs when DPI is changed, or form is moved to another monitor with different DPI
		/// </summary>
		public event OnDpiChangeEvent OnDpiChange;

		public HDpiForm()
		{
			_designMode = DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime;
			if (_designMode)
				return;

			Load += MainForm_Load;
			Move += MainForm_Move;
			AutoScaleDimensions = new SizeF(96F, 96F);
			AutoScaleMode = AutoScaleMode.Dpi;
		}

		// Catch window message of DPI change.
		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);

			if (_designMode) return;

			// Check if Windows 8.1 or newer and if not, ignore message.
			if (!IsEightOneOrNewer()) return;

			const int WM_DPICHANGED = 0x02e0; // 0x02E0 from WinUser.h

			if (m.Msg == WM_DPICHANGED)
			{
				// wParam
				short lo = Win32Api.GetLoWord(m.WParam.ToInt32());

				//// lParam
				//W32.RECT r = (W32.RECT)Marshal.PtrToStructure(m.LParam, typeof(W32.RECT));
				
				// Hold new DPI as target for adjustment.
				_newDpiValue = lo;

				MoveWindow();
				AdjustWindow();
			}
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			AutoScaleDimensions = new SizeF(96F, 96F);
			AutoScaleMode = AutoScaleMode.Dpi;
			AdjustWindowInitial();
		}

		// Detect this window is moved.
		private void MainForm_Move(object sender, EventArgs e)
		{
			if (_designMode) return;
			if (_willBeAdjusted && IsLocationGood())
			{
				_willBeAdjusted = false;

				AdjustWindow();
			}
		}
		// Adjust location, size and font size of Controls according to new DPI.
		private void AdjustWindowInitial()
		{
			if (_designMode) return;

			// Hold initial DPI used at loading this window.
			_oldDpiValue = CurrentAutoScaleDimensions.Width;

			// Check current DPI.
			_newDpiValue = GetDpiWindowMonitor();

			AdjustWindow();
		}

		// Adjust this window.
		private void AdjustWindow()
		{
			if ((_oldDpiValue == 0) || (_oldDpiValue == _newDpiValue)) return; // Abort.

			float factor = _newDpiValue / _oldDpiValue;

			_oldDpiValue = _newDpiValue;

			// Adjust location and size of Controls (except location of this window itself).
			Scale(new SizeF(factor, factor));

			// Adjust Font size of Controls.
			Font = new Font(Font.FontFamily,
				Font.Size * factor,
				Font.Style,
				Font.Unit);

			if (OnDpiChange != null)
				OnDpiChange(this, Font, _newDpiValue, factor);
		}
		
		// Get new location of this window after DPI change.
		private void MoveWindow()
		{
			if (_oldDpiValue == 0)
				return; // Abort.

			float factor = _newDpiValue / _oldDpiValue;

			// Prepare new rectangles shrinked or expanded sticking four corners.
			int widthDiff = (int)(ClientSize.Width * factor) - ClientSize.Width;
			int heightDiff = (int)(ClientSize.Height * factor) - ClientSize.Height;

			List<Win32Api.RECT> rectList = new List<Win32Api.RECT>();

			// Left-Top corner
			rectList.Add(new Win32Api.RECT
			{
				left = Bounds.Left,
				top = Bounds.Top,
				right = Bounds.Right + widthDiff,
				bottom = Bounds.Bottom + heightDiff
			});

			// Right-Top corner
			rectList.Add(new Win32Api.RECT
			{
				left = Bounds.Left - widthDiff,
				top = Bounds.Top,
				right = Bounds.Right,
				bottom = Bounds.Bottom + heightDiff
			});

			// Left-Bottom corner
			rectList.Add(new Win32Api.RECT
			{
				left = Bounds.Left,
				top = Bounds.Top - heightDiff,
				right = Bounds.Right + widthDiff,
				bottom = Bounds.Bottom
			});

			// Right-Bottom corner
			rectList.Add(new Win32Api.RECT
			{
				left = Bounds.Left - widthDiff,
				top = Bounds.Top - heightDiff,
				right = Bounds.Right,
				bottom = Bounds.Bottom
			});

			// Get handle to monitor that has the largest intersection with each rectangle.
			for (int i = 0; i <= rectList.Count - 1; i++)
			{
				Win32Api.RECT rectBuf = rectList[i];

				IntPtr handleMonitor = Win32Api.MonitorFromRect(ref rectBuf, Win32Api.MONITOR_DEFAULTTONULL);

				if (handleMonitor != IntPtr.Zero)
				{
					// Check if at least Left-Top corner or Right-Top corner is inside monitors.
					IntPtr handleLeftTop = Win32Api.MonitorFromPoint(new Win32Api.POINT(rectBuf.left, rectBuf.top), Win32Api.MONITOR_DEFAULTTONULL);
					IntPtr handleRightTop = Win32Api.MonitorFromPoint(new Win32Api.POINT(rectBuf.right, rectBuf.top), Win32Api.MONITOR_DEFAULTTONULL);

					if ((handleLeftTop != IntPtr.Zero) || (handleRightTop != IntPtr.Zero))
					{
						// Check if DPI of the monitor matches.
						if (GetDpiSpecifiedMonitor(handleMonitor) == _newDpiValue)
						{
							// Move this window.
							Location = new Point(rectBuf.left, rectBuf.top);
							break;
						}
					}
				}
			}
		}

		// Check if current location of this window is good for delayed adjustment.
		private bool IsLocationGood()
		{
			if (_oldDpiValue == 0)
				return false; // Abort.

			float factor = _newDpiValue / _oldDpiValue;

			// Prepare new rectangle shrinked or expanded sticking Left-Top corner.
			int widthDiff = (int)(ClientSize.Width * factor) - ClientSize.Width;
			int heightDiff = (int)(ClientSize.Height * factor) - ClientSize.Height;

			Win32Api.RECT rect = new Win32Api.RECT()
			{
				left = Bounds.Left,
				top = Bounds.Top,
				right = Bounds.Right + widthDiff,
				bottom = Bounds.Bottom + heightDiff
			};

			// Get handle to monitor that has the largest intersection with the rectangle.
			IntPtr handleMonitor = Win32Api.MonitorFromRect(ref rect, Win32Api.MONITOR_DEFAULTTONULL);

			if (handleMonitor != IntPtr.Zero)
			{
				// Check if DPI of the monitor matches.
				if (GetDpiSpecifiedMonitor(handleMonitor) == _newDpiValue)
				{
					return true;
				}
			}

			return false;
		}


		#region DPI

		// Get DPI of monitor containing this window by GetHDpiFormonitor.
		private float GetDpiWindowMonitor()
		{
			// Get handle to this window.
			// FIXED: Use this windows handle instead of Process.GetCurrentProcess().MainWindowHandl
			var handleWindow = this.Handle;

			// Get handle to monitor.
			IntPtr handleMonitor = Win32Api.MonitorFromWindow(handleWindow, Win32Api.MONITOR_DEFAULTTOPRIMARY);

			// Get DPI.
			return GetDpiSpecifiedMonitor(handleMonitor);
		}

		// Get DPI of a specified monitor by GetHDpiFormonitor.
		private float GetDpiSpecifiedMonitor(IntPtr handleMonitor)
		{
			// Check if GetHDpiFormonitor function is available.
			if (!IsEightOneOrNewer()) return CurrentAutoScaleDimensions.Width;

			// Get DPI.
			uint dpiX = 0;
			uint dpiY = 0;

			int result = Win32Api.GetHDpiFormonitor(handleMonitor, Win32Api.Monitor_DPI_Type.MDT_Default, out dpiX, out dpiY);

			if (result != 0) // If not S_OK (= 0)
			{
				throw new Exception("Failed to get DPI of monitor containing this window.");
			}

			return (float)dpiX;
		}

		// Get DPI for all monitors by GetDeviceCaps.
		private float GetDpiDeviceMonitor()
		{
			int dpiX = 0;
			IntPtr screen = IntPtr.Zero;

			try
			{
				screen = Win32Api.GetDC(IntPtr.Zero);
				dpiX = Win32Api.GetDeviceCaps(screen, Win32Api.LOGPIXELSX);
			}
			finally
			{
				if (screen != IntPtr.Zero)
				{
					Win32Api.ReleaseDC(IntPtr.Zero, screen);
				}
			}

			return (float)dpiX;
		}

		#endregion

		#region OS Version

		// Check if OS is Windows 8.1 or newer.
		private bool IsEightOneOrNewer()
		{
			// To get this value correctly, it is required to include ID of Windows 8.1 in the manifest file.
			return (6.3 <= GetVersion());
		}

		// Get OS version in Double.
		private double GetVersion()
		{
			var os = Environment.OSVersion;

			return os.Version.Major + ((double)os.Version.Minor / 10);
		}

		#endregion
	}
}
