using Eto.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Platform;
using System;
using Veldrid;
using Veldrid.OpenGL;

namespace Eto.Veldrid
{
	/// <summary>
	/// A simple control that allows drawing with Veldrid.
	/// </summary>
	[Handler(typeof(VeldridSurface.IHandler))]
	public class VeldridSurface : Control
	{
		public new interface IHandler : Control.IHandler
		{
			int RenderWidth { get; }
			int RenderHeight { get; }

			IWindowInfo WindowInfo { get; }

			Action<uint, uint> ResizeSwapchain { get; }

			Swapchain CreateSwapchain();
			IWindowInfo UpdateWindowInfo(GraphicsMode mode);
		}

		public new IHandler Handler => (IHandler)base.Handler;

		public new interface ICallback : Control.ICallback
		{
			void InitializeGraphicsBackend(VeldridSurface s);
			void OnDraw(VeldridSurface s, EventArgs e);
			void OnResize(VeldridSurface s, ResizeEventArgs e);
			void OnVeldridInitialized(VeldridSurface s, EventArgs e);
			void OnWindowInfoUpdated(VeldridSurface s, EventArgs e);
		}

		protected new class Callback : Control.Callback, ICallback
		{
			public void InitializeGraphicsBackend(VeldridSurface s) => s?.InitializeGraphicsBackend();
			public void OnDraw(VeldridSurface s, EventArgs e) => s?.OnDraw(e);
			public void OnResize(VeldridSurface s, ResizeEventArgs e) => s?.OnResize(e);
			public void OnVeldridInitialized(VeldridSurface s, EventArgs e) => s?.OnVeldridInitialized(e);
			public void OnWindowInfoUpdated(VeldridSurface s, EventArgs e) => s?.OnWindowInfoUpdated(e);
		}

		protected override object GetCallback() => new Callback();

		public static GraphicsBackend PreferredBackend { get; } = GetPreferredBackend();

		/// <summary>
		/// The render area's width, which may differ from the control's width
		/// (e.g. with high DPI displays).
		/// </summary>
		public int RenderWidth => Handler.RenderWidth;
		/// <summary>
		/// The render area's height, which may differ from the control's height
		/// (e.g. with high DPI displays).
		/// </summary>
		public int RenderHeight => Handler.RenderHeight;

		public GraphicsContext OpenTKGraphicsContext { get; protected set; }
		public OpenTKOptions OpenTKOptions { get; set; } = new OpenTKOptions(
			new GraphicsMode(new ColorFormat(32)),
			3,
			3,
			GraphicsContextFlags.ForwardCompatible);

		public GraphicsBackend Backend { get; set; } = PreferredBackend;
		public GraphicsDevice GraphicsDevice { get; set; }
		public GraphicsDeviceOptions GraphicsDeviceOptions { get; private set; } = new GraphicsDeviceOptions();
		public Swapchain Swapchain { get; set; }

		public const string VeldridInitializedEvent = "VeldridSurface.VeldridInitialized";
		public const string DrawEvent = "VeldridSurface.Draw";
		public const string ResizeEvent = "VeldridSurface.Resize";

		public event EventHandler<EventArgs> VeldridInitialized
		{
			add { Properties.AddHandlerEvent(VeldridInitializedEvent, value); }
			remove { Properties.RemoveEvent(VeldridInitializedEvent, value); }
		}
		public event EventHandler<EventArgs> Draw
		{
			add { Properties.AddHandlerEvent(DrawEvent, value); }
			remove { Properties.RemoveEvent(DrawEvent, value); }
		}
		public event EventHandler<ResizeEventArgs> Resize
		{
			add { Properties.AddHandlerEvent(ResizeEvent, value); }
			remove { Properties.RemoveEvent(ResizeEvent, value); }
		}

		public VeldridSurface()
		{
		}
		public VeldridSurface(GraphicsBackend backend)
		{
			Backend = backend;
		}
		public VeldridSurface(GraphicsBackend backend, GraphicsDeviceOptions gdOptions)
		{
			Backend = backend;
			GraphicsDeviceOptions = gdOptions;
		}
		public VeldridSurface(GraphicsBackend backend, GraphicsDeviceOptions gdOptions, OpenTKOptions tkOptions)
		{
			Backend = backend;
			GraphicsDeviceOptions = gdOptions;
			OpenTKOptions = tkOptions;
		}

		/// <summary>
		/// Initializes OpenTK; if your program will make use of Veldrid's
		/// OpenGL backend, this method must be called before creating your
		/// Eto.Forms.Application.
		/// </summary>
		public static void InitializeOpenTK()
		{
			// Ensure that OpenTK ignores SDL2 if it's installed.
			var options = new ToolkitOptions { Backend = PlatformBackend.PreferNative };

			// This is technically only important for OpenGL, as it's the only
			// Veldrid backend that uses OpenTK, but since Veldrid also allows
			// live switching of backends, it's worth doing regardless of which
			// one users start out with. Anyone who plans to completely avoid
			// OpenGL is free to simply not call InitializeOpenTK at all.
			Toolkit.Init(options);
		}

		private static GraphicsBackend GetPreferredBackend()
		{
			GraphicsBackend? backend = null;

			// It'd be less ugly to just loop through the GraphicsBackend
			// enum, but the backends aren't arranged in an ideal order,
			// either ascending or descending. The below progression is only
			// a judgment call, and could easily get rearranged if need be.
			foreach (GraphicsBackend b in new[] {
				GraphicsBackend.Metal,
				GraphicsBackend.Vulkan,
				GraphicsBackend.Direct3D11,
				GraphicsBackend.OpenGL,
				GraphicsBackend.OpenGLES })
			{
				bool supported = false;

				try
				{
					supported = GraphicsDevice.IsBackendSupported(b);
				}
				catch (InvalidOperationException)
				{
					// Veldrid, as of 4.7.0, throws this exception when
					// trying to test for Vulkan in macOS if it's not
					// available on the system.
				}

				if (supported)
				{
					backend = b;

					// With backends being checked from most to least
					// desirable, it's important to break as soon as a
					// suitable backend is detected.
					break;
				}
			}

			if (backend == null)
			{
				throw new VeldridException("VeldridSurface: No supported Veldrid backend found!");
			}

			return (GraphicsBackend)backend;
		}

		private void InitializeGraphicsBackend()
		{
			switch (Backend)
			{
				case GraphicsBackend.Metal:
					GraphicsDevice = GraphicsDevice.CreateMetal(GraphicsDeviceOptions);
					break;
				case GraphicsBackend.Vulkan:
					GraphicsDevice = GraphicsDevice.CreateVulkan(GraphicsDeviceOptions);
					break;
				case GraphicsBackend.Direct3D11:
					GraphicsDevice = GraphicsDevice.CreateD3D11(GraphicsDeviceOptions);
					break;
				case GraphicsBackend.OpenGL:
					Handler.UpdateWindowInfo(OpenTKOptions.Mode);

					var glInfo = new OpenGLPlatformInfo(
						VeldridGL.GetGLContextHandle(),
						VeldridGL.GetProcAddress,
						(c) => OpenTKGraphicsContext.MakeCurrent(Handler.WindowInfo),
						VeldridGL.GetCurrentContext,
						VeldridGL.ClearCurrentContext,
						VeldridGL.DeleteContext,
						VeldridGL.SwapBuffers,
						VeldridGL.SetVSync,
						VeldridGL.SetSwapchainFramebuffer,
						Handler.ResizeSwapchain);

					GraphicsDevice = GraphicsDevice.CreateOpenGL(
						GraphicsDeviceOptions,
						glInfo,
						(uint)RenderWidth,
						(uint)RenderHeight);

					break;
				default:
					string message;
					if (!Enum.IsDefined(typeof(GraphicsBackend), Backend))
					{
						message = "Unrecognized backend!";
					}
					else
					{
						message = "Specified backend not supported on this platform!";
					}

					throw new ArgumentException(message);
			}

			Swapchain = Handler.CreateSwapchain();

			OnVeldridInitialized(EventArgs.Empty);
		}

		protected virtual void OnDraw(EventArgs e) => Properties.TriggerEvent(DrawEvent, this, e);

		protected virtual void OnResize(ResizeEventArgs e)
		{
			if (Swapchain != null && e != null)
			{
				Swapchain.Resize((uint)e.Width, (uint)e.Height);
			}

			Properties.TriggerEvent(ResizeEvent, this, e);
		}

		protected virtual void OnVeldridInitialized(EventArgs e) => Properties.TriggerEvent(VeldridInitializedEvent, this, e);

		protected virtual void OnWindowInfoUpdated(EventArgs e)
		{
			if (Backend != GraphicsBackend.OpenGL)
			{
				return;
			}

			if (OpenTKGraphicsContext == null)
			{
				OpenTKGraphicsContext = new GraphicsContext(
					OpenTKOptions.Mode,
					Handler.WindowInfo,
					OpenTKOptions.MajorVersion,
					OpenTKOptions.MinorVersion,
					OpenTKOptions.Flags);
			}
			else
			{
				OpenTKGraphicsContext.Update(Handler.WindowInfo);
			}

			OpenTKGraphicsContext.MakeCurrent(Handler.WindowInfo);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			if (!Loaded)
			{
				return;
			}

			OnResize(new ResizeEventArgs(RenderWidth, RenderHeight));
		}
	}
}
