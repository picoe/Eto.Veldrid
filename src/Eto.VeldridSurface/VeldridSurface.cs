using Eto;
using Eto.Drawing;
using Eto.Forms;
using OpenTK.Graphics;
using System;
using System.Reflection;
using Veldrid;

// TODO: Come up with a suitable namespace. Eto.Veldrid will conflict with the
// global Veldrid, and Eto.VeldridSurface makes the VeldridSurface class harder
// to use. Suggestions welcome!
namespace PlaceholderName
{
	/// <summary>
	/// A collection of helper methods to grant Veldrid access to an OpenGL context.
	/// </summary>
	/// <remarks>
	/// These are potentially dangerous methods, so please don't try to use them
	/// for your own purposes. Hand them off to Veldrid as VeldridSurfaceHandler
	/// does in InitializeOpenGL, and forget about them.
	/// </remarks>
	public static class VeldridGL
	{
		static readonly GraphicsContext.GetAddressDelegate _getProcAddress;

		static VeldridGL()
		{
			// Depending on a private method like CreateGetAddress is hardly in
			// line with best practices, but it's the only way to grant Veldrid
			// access to OpenTK's internals. It's not pretty, but this Eto
			// integration was designed specifically for OpenTK 3.x, so sticking
			// to that branch should ensure private things stay where they are.
			Type type = typeof(OpenTK.Platform.Utilities);
			MethodInfo createGetAddress = type.GetMethod("CreateGetAddress", BindingFlags.NonPublic | BindingFlags.Static);
			_getProcAddress = (GraphicsContext.GetAddressDelegate)createGetAddress.Invoke(null, Array.Empty<string>());
		}

		// TODO: Find out if this is correct! The docs just say that the
		// 'openGLContextHandle' parameter of an OpenGLPlatformInfo is, and I
		// quote, "The OpenGL context handle", which isn't terribly helpful. Doing
		// this seems to work, but then if all Veldrid needs is the current
		// context, it could use the method I specify for GetCurrentContext,
		// couldn't it? There must be more to this.
		public static IntPtr GetGLContextHandle() => GetCurrentContext();

		public static IntPtr GetProcAddress(string name) => _getProcAddress.Invoke(name);

		public static IntPtr GetCurrentContext() => GraphicsContext.CurrentContextHandle.Handle;

		public static void ClearCurrentContext() => GraphicsContext.CurrentContext.MakeCurrent(null);

		public static void DeleteContext(IntPtr context)
		{
			// TODO: Update this! Eto.Gl/Eto.OpenTK is no longer in use, so I'll need
			// to dispose the specified context manually. I think.

			// Do nothing! With this Eto.Gl-based approach, Veldrid should never
			// need to destroy an OpenGL context on its own; let the GLSurface
			// take care of context deletion upon its own disposal.
		}

		public static void SwapBuffers() => GraphicsContext.CurrentContext.SwapBuffers();

		public static void SetVSync(bool on) => GraphicsContext.CurrentContext.SwapInterval = on ? 1 : 0;

		// It's perfectly acceptable to create an instance of OpenGLPlatformInfo
		// without providing these last two methods, if indeed you don't need
		// them. They're stubbed out here only to serve as a reminder that they
		// can be customized should the occasion call for it.

		public static void SetSwapchainFramebuffer()
		{
		}

		public static void ResizeSwapchain(uint width, uint height)
		{
		}
	}

	public class ResizeEventArgs : EventArgs
	{
		public int Width { get; set; }
		public int Height { get; set; }

		public ResizeEventArgs()
		{
		}
		public ResizeEventArgs(int width, int height)
		{
			Width = width;
			Height = height;
		}
		public ResizeEventArgs(Size size)
		{
			Width = size.Width;
			Height = size.Height;
		}
	}

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

			void InitializeOpenGL();
			void InitializeOtherApi();
		}

		public new IHandler Handler => (IHandler)base.Handler;

		public new interface ICallback : Control.ICallback
		{
			void OnControlReady(VeldridSurface s, EventArgs e);
			void OnDraw(VeldridSurface s, EventArgs e);
			void OnOpenGLReady(VeldridSurface s, EventArgs e);
			void OnResize(VeldridSurface s, ResizeEventArgs e);
			void OnVeldridInitialized(VeldridSurface s, EventArgs e);
		}

		protected new class Callback : Control.Callback, ICallback
		{
			public void OnControlReady(VeldridSurface s, EventArgs e)
			{
				s.ControlReady = true;
			}

			public void OnDraw(VeldridSurface s, EventArgs e)
			{
				s.OnDraw(e);
			}

			public void OnOpenGLReady(VeldridSurface s, EventArgs e)
			{
				s.OpenGLReady = true;
			}

			public void OnResize(VeldridSurface s, ResizeEventArgs e)
			{
				s.OnResize(e);
			}

			public void OnVeldridInitialized(VeldridSurface s, EventArgs e)
			{
				s.OnVeldridInitialized(e);
			}
		}

		protected override object GetCallback()
		{
			return new Callback();
		}

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

		// A depth buffer isn't strictly necessary for this project, which uses
		// only 2D vertex coordinates, but it's helpful to create one for the
		// sake of demonstration.
		//
		// The "improved" resource binding model changes how resource slots are
		// assigned in the Metal backend, allowing it to work like the others,
		// so the numbers used in calls to CommandList.SetGraphicsResourceSet
		// will make more sense to developers used to e.g. OpenGL or Direct3D.
		public GraphicsDeviceOptions GraphicsDeviceOptions { get; } =
			new GraphicsDeviceOptions(
				false,
				Veldrid.PixelFormat.R32_Float,
				false,
				ResourceBindingModel.Improved);

		public static GraphicsBackend PreferredBackend
		{
			get
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
		}

		public GraphicsBackend Backend { get; set; }

		public GraphicsDevice GraphicsDevice { get; set; }
		public Swapchain Swapchain { get; set; }

		private bool _controlReady = false;
		public bool ControlReady
		{
			get { return _controlReady; }
			private set
			{
				_controlReady = value;

				InitializeGraphicsApi();
			}
		}

		private bool? _openGLReady = null;
		public bool? OpenGLReady
		{
			get { return _openGLReady; }
			private set
			{
				_openGLReady = value;

				InitializeGraphicsApi();
			}
		}

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
		public event EventHandler<EventArgs> Resize
		{
			add { Properties.AddHandlerEvent(ResizeEvent, value); }
			remove { Properties.RemoveEvent(ResizeEvent, value); }
		}

		public VeldridSurface() : this(PreferredBackend)
		{
		}
		public VeldridSurface(GraphicsBackend backend)
		{
			Backend = backend;

			if (Backend == GraphicsBackend.OpenGL)
			{
				OpenGLReady = false;
			}
		}

		public void InitializeGraphicsApi()
		{
			if (!ControlReady)
			{
				return;
			}

			switch (OpenGLReady)
			{
				case false:
					return;
				case true:
					Handler.InitializeOpenGL();
					break;
				case null:
					InitializeOtherApi();
					break;
			}

			// Ideally Callback.OnVeldridInitialized would be called here, but
			// WPF needs to delay raising that event until after its control has
			// been Loaded. Each platform's XVeldridSurfaceHandler therefore has
			// to call OnVeldridInitialized itself.
		}

		private void InitializeOtherApi()
		{
			if (Backend == GraphicsBackend.Metal)
			{
				GraphicsDevice = GraphicsDevice.CreateMetal(GraphicsDeviceOptions);
			}
			else if (Backend == GraphicsBackend.Vulkan)
			{
				GraphicsDevice = GraphicsDevice.CreateVulkan(GraphicsDeviceOptions);
			}
			else if (Backend == GraphicsBackend.Direct3D11)
			{
				GraphicsDevice = GraphicsDevice.CreateD3D11(GraphicsDeviceOptions);
			}
			else
			{
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

			Handler.InitializeOtherApi();
		}

		protected virtual void OnVeldridInitialized(EventArgs e)
		{
			Properties.TriggerEvent(VeldridInitializedEvent, this, e);
		}
		protected virtual void OnDraw(EventArgs e)
		{
			Properties.TriggerEvent(DrawEvent, this, e);
		}
		protected virtual void OnResize(ResizeEventArgs e)
		{
			Swapchain?.Resize((uint)e.Width, (uint)e.Height);

			Properties.TriggerEvent(ResizeEvent, this, e);
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
