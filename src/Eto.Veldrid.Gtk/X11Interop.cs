using System;
using System.Runtime.InteropServices;

namespace Eto.Veldrid.Gtk
{
	internal static class X11Interop
	{
		const string
			libX11_name = "libX11",
#if GTK3
			linux_libgdk_x11_name = "libgdk-3.so.0",
#else
			linux_libgdk_x11_name = "libgdk-x11-2.0.so.0",
#endif
			linux_libGL_name = "libGL.so.1",
			linux_libX11_name = "libX11.so.6";

		public enum XVisualClass
		{
			StaticGray,
			GrayScale,
			StaticColor,
			PseudoColor,
			TrueColor,
			DirectColor
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct XVisualInfo
		{
			public IntPtr Visual;
			public IntPtr VisualID;
			public int Screen;
			public int Depth;
			public XVisualClass Class;
			public long RedMask;
			public long GreenMask;
			public long BlueMask;
			public int ColormapSize;
			public int BitsPerRgb;

			public override string ToString() => $"VisualID {VisualID}, Screen {Screen}, Depth {Depth}, Class {Class}";
		}

		[Flags]
		public enum XVisualInfoMask
		{
			None = 0x0,
			ID = 0x1,
			Screen = 0x2,
			Depth = 0x4,
			Class = 0x8,
			Red = 0x10,
			Green = 0x20,
			Blue = 0x40,
			ColormapSize = 0x80,
			BitsPerRgb = 0x100,
			All = 0x1FF
		}

		public const int
			GLX_NONE = 0,
			GLX_USE_GL = 1,
			GLX_BUFFER_SIZE = 2,
			GLX_LEVEL = 3,
			GLX_RGBA = 4,
			GLX_DOUBLEBUFFER = 5,
			GLX_STEREO = 6,
			GLX_AUX_BUFFERS = 7,
			GLX_RED_SIZE = 8,
			GLX_GREEN_SIZE = 9,
			GLX_BLUE_SIZE = 10,
			GLX_ALPHA_SIZE = 11,
			GLX_DEPTH_SIZE = 12,
			GLX_STENCIL_SIZE = 13,
			GLX_ACCUM_RED_SIZE = 14,
			GLX_ACCUM_GREEN_SIZE = 15,
			GLX_ACCUM_BLUE_SIZE = 16,
			GLX_ACCUM_ALPHA_SIZE = 17;

		public static IntPtr GetVisualInfo(IntPtr display, int screen)
		{
			try
			{
				return glXChooseVisual(display, screen, new int[] {
					GLX_RGBA,
					GLX_RED_SIZE, 8,
					GLX_GREEN_SIZE, 8,
					GLX_BLUE_SIZE, 8,
					GLX_ALPHA_SIZE, 8,
					GLX_DEPTH_SIZE, 8,
					GLX_STENCIL_SIZE, 8,
					GLX_NONE
				});
			}
			catch (DllNotFoundException e)
			{
				throw new DllNotFoundException("OpenGL library not found!", e);
			}
			catch (EntryPointNotFoundException e)
			{
				throw new EntryPointNotFoundException("glX entry point not found!", e);
			}
		}

		[DllImport(linux_libgdk_x11_name)]
		static public extern IntPtr gdk_x11_display_get_xdisplay(IntPtr gdkDisplay);

		[DllImport(linux_libgdk_x11_name)]
		static public extern int gdk_x11_screen_get_screen_number(IntPtr gdkScreen);

#if GTK3
		[DllImport(linux_libgdk_x11_name)]
		static public extern IntPtr gdk_x11_window_get_xid(IntPtr gdkDisplay);
#else	
		[DllImport(linux_libgdk_x11_name)]
		static public extern IntPtr gdk_x11_drawable_get_xid(IntPtr gdkDisplay);
#endif

		[DllImport(linux_libGL_name)]
		static public extern IntPtr glXChooseVisual(IntPtr display, int screen, int[] attr);

		[DllImport(linux_libX11_name)]
		static public extern void XFree(IntPtr handle);

		[DllImport(libX11_name, EntryPoint = "XGetVisualInfo")]
		static public extern IntPtr XGetVisualInfo(IntPtr display, IntPtr vinfo_mask, ref XVisualInfo vinfo_template, out int nitems_return);
	}
}
