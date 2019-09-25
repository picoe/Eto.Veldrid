using Eto.Forms;
using Eto.GtkSharp.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Platform;
using System;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.OpenGL;

namespace PlaceholderName
{
	internal static class X11Interop
	{
		const string
			libX11_name = "libX11",
			linux_libgdk_x11_name = "libgdk-x11-2.0.so.0",
			linux_libgl_name = "libGL.so.1",
			linux_libx11_name = "libX11.so.6";

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

			public override string ToString()
			{
				return $"VisualID {VisualID}, Screen {Screen}, Depth {Depth}, Class {Class}";
			}
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
					GLX_RED_SIZE,
					8,
					GLX_GREEN_SIZE,
					8,
					GLX_BLUE_SIZE,
					8,
					GLX_ALPHA_SIZE,
					8,
					GLX_DEPTH_SIZE,
					8,
					GLX_STENCIL_SIZE,
					8,
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

		[DllImport(libX11_name, EntryPoint = ("XGetVisualInfo"))]
		static public extern IntPtr XGetVisualInfo(IntPtr display, IntPtr vinfo_mask, ref XVisualInfo vinfo_template, out int nitems_return);

		[DllImport(linux_libgdk_x11_name)]
		static public extern IntPtr gdk_x11_display_get_xdisplay(IntPtr gdkDisplay);

		[DllImport(linux_libgdk_x11_name)]
		static public extern IntPtr gdk_x11_drawable_get_xid(IntPtr gdkDisplay);

		[DllImport(linux_libgl_name)]
		static public extern IntPtr glXChooseVisual(IntPtr display, int screen, int[] attr);

		[DllImport(linux_libx11_name)]
		static public extern void XFree(IntPtr handle);
	}

	public class GtkVeldridDrawingArea : DrawingArea
	{
		GraphicsMode Mode = new GraphicsMode(new ColorFormat(32), 8, 8);

		public IWindowInfo WindowInfo { get; set; }
		public GraphicsContext Context { get; private set; }

		public GtkVeldridDrawingArea()
		{
			CanFocus = true;
		}

		public void CreateOpenGLContext()
		{
			IntPtr display = X11Interop.gdk_x11_display_get_xdisplay(Display.Handle);
			int screen = Screen.Number;

			IntPtr visualInfo;
			if (Mode.Index.HasValue)
			{
				var info = new X11Interop.XVisualInfo { VisualID = Mode.Index.Value };

				visualInfo = X11Interop.XGetVisualInfo(display, (IntPtr)(int)X11Interop.XVisualInfoMask.ID, ref info, out int dummy);
			}
			else
			{
				visualInfo = X11Interop.GetVisualInfo(display, screen);
			}

			WindowInfo = Utilities.CreateX11WindowInfo(
				display,
				screen,
				X11Interop.gdk_x11_drawable_get_xid(GdkWindow.Handle),
				X11Interop.gdk_x11_drawable_get_xid(RootWindow.Handle),
				visualInfo);

			X11Interop.XFree(visualInfo);

			Context = new GraphicsContext(Mode, WindowInfo, 3, 3, GraphicsContextFlags.ForwardCompatible);

			Context.MakeCurrent(WindowInfo);
		}

		public void MakeCurrent(IntPtr context)
		{
			Context.MakeCurrent(WindowInfo);
		}
	}

	public class GtkVeldridSurfaceHandler : GtkControl<GtkVeldridDrawingArea, VeldridSurface, VeldridSurface.ICallback>, VeldridSurface.IHandler
	{
		public new VeldridSurface.ICallback Callback => base.Callback;
		public new VeldridSurface Widget => base.Widget;

		// TODO: Find out if Gtk2 even supports different DPI settings, and if
		// so test it out and get this to return the correct values.
		public int RenderWidth => Widget.Width;
		public int RenderHeight => Widget.Height;

		public GtkVeldridSurfaceHandler()
		{
			Control = new GtkVeldridDrawingArea();

			Control.ExposeEvent += Control_ExposeEvent;
		}

		public override void AttachEvent(string id)
		{
			switch (id)
			{
				case VeldridSurface.DrawEvent:
					Control.ExposeEvent += (sender, e) => Callback.OnDraw(Widget, e);
					break;
				default:
					base.AttachEvent(id);
					break;
			}
		}

		void Control_ExposeEvent(object o, ExposeEventArgs args)
		{
			if (Widget.Backend == GraphicsBackend.OpenGL)
			{
				Control.CreateOpenGLContext();

				Callback.OnOpenGLReady(Widget, EventArgs.Empty);
			}

			Control.ExposeEvent -= Control_ExposeEvent;

			Callback.OnControlReady(Widget, EventArgs.Empty);
		}

		/// <summary>
		/// Prepare this VeldridSurface to use OpenGL.
		/// </summary>
		public void InitializeOpenGL()
		{
			var platformInfo = new OpenGLPlatformInfo(
				VeldridGL.GetGLContextHandle(),
				VeldridGL.GetProcAddress,
				Control.MakeCurrent,
				VeldridGL.GetCurrentContext,
				VeldridGL.ClearCurrentContext,
				VeldridGL.DeleteContext,
				VeldridGL.SwapBuffers,
				VeldridGL.SetVSync,
				VeldridGL.SetSwapchainFramebuffer,
				VeldridGL.ResizeSwapchain);

			Widget.GraphicsDevice = GraphicsDevice.CreateOpenGL(
				Widget.GraphicsDeviceOptions,
				platformInfo,
				(uint)RenderWidth,
				(uint)RenderHeight);

			Widget.Swapchain = Widget.GraphicsDevice.MainSwapchain;

			Callback.OnVeldridInitialized(Widget, EventArgs.Empty);
		}

		public void InitializeOtherApi()
		{
			// To embed Veldrid in an Eto control, all these platform-specific
			// versions of InitializeOtherApi use the technique outlined here:
			//
			//   https://github.com/mellinoe/veldrid/issues/155
			//
			var source = SwapchainSource.CreateXlib(
				X11Interop.gdk_x11_display_get_xdisplay(Control.Display.Handle),
				X11Interop.gdk_x11_drawable_get_xid(Control.GdkWindow.Handle));

			Widget.Swapchain = Widget.GraphicsDevice.ResourceFactory.CreateSwapchain(
				new SwapchainDescription(
					source,
					(uint)RenderWidth,
					(uint)RenderHeight,
					PixelFormat.R32_Float,
					false));

			Callback.OnVeldridInitialized(Widget, EventArgs.Empty);
		}
	}

	public static class MainClass
	{
		[STAThread]
		public static void Main(string[] args)
		{
			GraphicsBackend backend = VeldridSurface.PreferredBackend;

			if (backend == GraphicsBackend.OpenGL)
			{
				Toolkit.Init(new ToolkitOptions { Backend = PlatformBackend.PreferNative });
			}

			var platform = new Eto.GtkSharp.Platform();
			platform.Add<VeldridSurface.IHandler>(() => new GtkVeldridSurfaceHandler());

			new Eto.Forms.Application(platform).Run(new MainForm(backend));
		}
	}
}
