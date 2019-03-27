using Eto.VeldridSurface;
using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace PlaceholderName
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;

		public int X
		{
			get { return Left; }
			set
			{
				Right -= (Left - value);
				Left = value;
			}
		}
		public int Y
		{
			get { return Top; }
			set
			{
				Bottom -= (Top - value);
				Top = value;
			}
		}

		public System.Drawing.Point Location
		{
			get { return new System.Drawing.Point(Left, Top); }
			set
			{
				X = value.X;
				Y = value.Y;
			}
		}

		public int Width
		{
			get { return Right - Left; }
			set { Right = value + Left; }
		}
		public int Height
		{
			get { return Bottom - Top; }
			set { Bottom = value + Top; }
		}

		public System.Drawing.Size Size
		{
			get { return new System.Drawing.Size(Width, Height); }
			set
			{
				Width = value.Width;
				Height = value.Height;
			}
		}

		public RECT(System.Drawing.Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom)
		{
		}
		public RECT(int left, int top, int right, int bottom)
		{
			Left = left;
			Top = top;
			Right = right;
			Bottom = bottom;
		}

		public static implicit operator System.Drawing.Rectangle(RECT r)
		{
			return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
		}

		public static implicit operator RECT(System.Drawing.Rectangle r)
		{
			return new RECT(r);
		}

		public bool Equals(RECT r)
		{
			return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
		}

		public override bool Equals(object other)
		{
			if (other is RECT)
			{
				return Equals((RECT)other);
			}
			else if (other is System.Drawing.Rectangle)
			{
				return Equals(new RECT((System.Drawing.Rectangle)other));
			}

			return false;
		}

		public static bool operator ==(RECT lhs, RECT rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(RECT lhs, RECT rhs)
		{
			return !lhs.Equals(rhs);
		}

		public override int GetHashCode()
		{
			return ((System.Drawing.Rectangle)this).GetHashCode();
		}

		public override string ToString()
		{
			return String.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct PAINTSTRUCT
	{
		public IntPtr hdc;
		public bool fErase;
		public RECT rcPaint;
		public bool fRestore;
		public bool fIncUpdate;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] rgbReserved;
	}

	// This is a barebones implementation, based on what can be found at
	// https://docs.microsoft.com/en-us/dotnet/framework/wpf/advanced/walkthrough-hosting-a-win32-control-in-wpf
	public class WpfVeldridHost : HwndHost
	{
		public static int
			WS_CHILD = 0x40000000,
			WS_VISIBLE = 0x10000000,
			HOST_ID = 0x00000002;

		public IntPtr Hwnd { get; private set; }

		public event EventHandler<EventArgs> WmPaint;
		public event EventHandler<ResizeEventArgs> WmSize;

		protected override HandleRef BuildWindowCore(HandleRef hwndParent)
		{
			Hwnd = CreateWindowEx(
				0,
				"static",
				"",
				WS_CHILD | WS_VISIBLE,
				0, 0,
				640, 480,
				hwndParent.Handle,
				(IntPtr)HOST_ID,
				IntPtr.Zero,
				0);

			return new HandleRef(this, Hwnd);
		}

		protected override void DestroyWindowCore(HandleRef hwnd)
		{
			DestroyWindow(hwnd.Handle);
		}

		public static int
			WM_SIZE = 5,
			WM_PAINT = 15;

		protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (msg == WM_SIZE)
			{
				int width = (short)lParam.ToInt32();
				int height = lParam.ToInt32() >> 16;

				OnWmSize(new ResizeEventArgs { Width = width, Height = height });

				handled = true;
			}
			else if (msg == WM_PAINT)
			{
				BeginPaint(Hwnd, out PAINTSTRUCT ps);
				OnWmPaint(EventArgs.Empty);
				EndPaint(Hwnd, ref ps);

				handled = true;
			}
			else
			{
				handled = false;
			}

			return IntPtr.Zero;
		}

		protected virtual void OnWmPaint(EventArgs e)
		{
			WmPaint?.Invoke(this, e);
		}
		protected virtual void OnWmSize(ResizeEventArgs e)
		{
			WmSize?.Invoke(this, e);
		}

		[DllImport("user32.dll", EntryPoint = "CreateWindowEx", CharSet = CharSet.Unicode)]
		internal static extern IntPtr CreateWindowEx(
			int dwExStyle,
			string lpszClassName,
			string lpszWindowName,
			int style,
			int x, int y,
			int width, int height,
			IntPtr hwndParent,
			IntPtr hMenu,
			IntPtr hInst,
			[MarshalAs(UnmanagedType.AsAny)] object pvParam);

		[DllImport("user32.dll", EntryPoint = "DestroyWindow", CharSet = CharSet.Unicode)]
		internal static extern bool DestroyWindow(IntPtr hwnd);

		[DllImport("user32.dll", EntryPoint = "BeginPaint", CharSet = CharSet.Unicode)]
		internal static extern IntPtr BeginPaint(IntPtr hwnd, out PAINTSTRUCT lpPaint);

		[DllImport("user32.dll", EntryPoint = "EndPaint", CharSet = CharSet.Unicode)]
		internal static extern bool EndPaint(IntPtr hwnd, [In] ref PAINTSTRUCT lpPaint);
	}
}
