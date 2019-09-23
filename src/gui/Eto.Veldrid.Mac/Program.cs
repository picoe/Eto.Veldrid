using Eto.Forms;
using Eto.Mac.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Platform;
using System;
using System.Diagnostics;
using System.IO;
using Veldrid;
using Veldrid.OpenGL;

#if MONOMAC
using MonoMac.AppKit;
using MonoMac.CoreGraphics;
#elif XAMMAC2
using AppKit;
using CoreGraphics;
#endif

namespace PlaceholderName
{
	public class MacVeldridView : NSView, IMacControl
	{
		public override bool AcceptsFirstMouse(NSEvent theEvent)
		{
			return CanFocus;
		}

		public override bool AcceptsFirstResponder()
		{
			return CanFocus;
		}

		public bool CanFocus { get; set; } = true;

		public WeakReference WeakHandler { get; set; }

		GraphicsMode Mode = new GraphicsMode(new ColorFormat(32), 8, 8);

		public IWindowInfo WindowInfo { get; set; }
		public GraphicsContext Context { get; private set; }

		public event EventHandler Draw;
		public event EventHandler OpenGLContextCreated;

		public void CreateOpenGLContext()
		{
			WindowInfo = Utilities.CreateMacOSWindowInfo(Window.Handle, Handle);

			Context = new GraphicsContext(Mode, WindowInfo, 3, 3, GraphicsContextFlags.ForwardCompatible);

			Context.MakeCurrent(WindowInfo);
		}

		public void MakeCurrent(IntPtr context)
		{
			Context.MakeCurrent(WindowInfo);
		}

		public void UpdateContext()
		{
			WindowInfo = Utilities.CreateMacOSWindowInfo(Window.Handle, Handle);

			Context?.Update(WindowInfo);
		}

		public override void DidChangeBackingProperties()
		{
			base.DidChangeBackingProperties();

			UpdateContext();
		}

		public override void DrawRect(CGRect dirtyRect)
		{
			Draw?.Invoke(this, EventArgs.Empty);
		}
	}

	public class MacVeldridSurfaceHandler : MacView<MacVeldridView, VeldridSurface, VeldridSurface.ICallback>, VeldridSurface.IHandler
	{
		public new VeldridSurface.ICallback Callback => (VeldridSurface.ICallback)base.Callback;
		public new VeldridSurface Widget => (VeldridSurface)base.Widget;

		// TODO: Set up some way to test HiDPI in macOS and figure out how to
		// get the right values here.
		public int RenderWidth => Widget.Width;
		public int RenderHeight => Widget.Height;

		public override NSView ContainerControl => Control;

		public override bool Enabled { get; set; }

		public MacVeldridSurfaceHandler()
		{
			Control = new MacVeldridView();

			Control.Draw += Control_Draw;
		}

		public override void AttachEvent(string id)
		{
			switch (id)
			{
				case VeldridSurface.DrawEvent:
					Control.Draw += (sender, e) => Callback.OnDraw(Widget, e);
					break;
				default:
					base.AttachEvent(id);
					break;
			}
		}

		private void Control_Draw(object sender, EventArgs e)
		{
			if (Widget.Backend == GraphicsBackend.OpenGL)
			{
				Control.CreateOpenGLContext();

				Callback.OnOpenGLReady(Widget, EventArgs.Empty);
			}

			Control.Draw -= Control_Draw;

			Callback.OnControlReady(Widget, EventArgs.Empty);
		}

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
				(w, h) => Control.UpdateContext());

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
			var source = SwapchainSource.CreateNSView(Control.Handle);

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

			var platform = new Eto.Mac.Platform();
			platform.Add<VeldridSurface.IHandler>(() => new MacVeldridSurfaceHandler());

			// AppContext.BaseDirectory is too simple for the case of the Mac
			// projects. When building an app bundle that depends on the Mono
			// framework being installed, it properly returns the path of the
			// executable in Eto.Veldrid.app/Contents/MacOS. When building an
			// app bundle that instead bundles Mono by way of mkbundle, on the
			// other hand, it returns the directory containing the .app.
			new Application(platform).Run(new MainForm(
				backend,
				Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName),
				Path.Combine("..", "Resources", "shaders")));
		}
	}
}
