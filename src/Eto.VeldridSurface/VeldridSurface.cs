using Eto;
using Eto.Drawing;
using Eto.Forms;
using Eto.Gl;
using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Reflection;
using Veldrid;
using Veldrid.OpenGL;

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
		public static Dictionary<IntPtr, GLSurface> Contexts { get; } = new Dictionary<IntPtr, GLSurface>();

		public static List<GLSurface> Surfaces { get; } = new List<GLSurface>();

		public static IntPtr GetGLContextHandle()
		{
			return GetCurrentContext();
		}

		// It should be noted that both GetProcAddress and MakeCurrent aren't
		// exactly following best practices here; they depend on a private
		// method and field, respectively, to grant Veldrid access to OpenTK's
		// internals. It's not pretty, but since this Eto integration was
		// designed specifically with OpenTK 3.0.1 in mind, sticking with that
		// version will guarantee that anything private will stay where it is.

		public static IntPtr GetProcAddress(string name)
		{
			Type type = typeof(OpenTK.Platform.Utilities);

			MethodInfo createGetAddress = type.GetMethod("CreateGetAddress", BindingFlags.NonPublic | BindingFlags.Static);
			var getAddress = (GraphicsContext.GetAddressDelegate)createGetAddress.Invoke(null, Array.Empty<string>());

			return getAddress.Invoke(name);
		}

		public static void MakeCurrent(IntPtr context)
		{
			Type type = typeof(GraphicsContext);

			var available = (Dictionary<ContextHandle, IGraphicsContext>)type
				.GetField("available_contexts", BindingFlags.NonPublic | BindingFlags.Static)
				.GetValue(null);

			bool found = false;
			foreach (KeyValuePair<ContextHandle, IGraphicsContext> pair in available)
			{
				foreach (GLSurface s in Surfaces)
				{
					if (pair.Key.Handle == context)
					{
						if (!Contexts.ContainsKey(context))
						{
							Contexts.Add(context, s);
						}
						Contexts[context].MakeCurrent();

						found = true;
					}

					if (found)
					{
						break;
					}
				}

				if (found)
				{
					break;
				}
			}
		}

		public static IntPtr GetCurrentContext()
		{
			return GraphicsContext.CurrentContextHandle.Handle;
		}

		public static void ClearCurrentContext()
		{
			GraphicsContext.CurrentContext.MakeCurrent(null);
		}

		public static void DeleteContext(IntPtr context)
		{
			// Do nothing! With this Eto.Gl-based approach, Veldrid should never
			// need to destroy an OpenGL context on its own; let the GLSurface
			// take care of context deletion upon its own disposal.
		}

		public static void SwapBuffers()
		{
			GraphicsContext.CurrentContext.SwapBuffers();
		}

		public static void SetVSync(bool on)
		{
			GraphicsContext.CurrentContext.SwapInterval = on ? 1 : 0;
		}

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

	// VeldridSurface only has a few needs that are different for each platform
	// offered by Eto, so a "themed" handler based on Panel takes care of all
	// the common busywork. Derived classes, e.g. WinFormsVeldridSurfaceHandler,
	// provide the platform-specific code necessary to get up and running.
	public class VeldridSurfaceHandler : ThemedControlHandler<Panel, VeldridSurface, VeldridSurface.ICallback>, VeldridSurface.IHandler
	{
		public Control RenderTarget
		{
			get { return Control.Content; }
			set { Control.Content = value; }
		}

		public VeldridSurfaceHandler()
		{
			Control = new Panel();
		}

		public override void AttachEvent(string id)
		{
			switch (id)
			{
				case VeldridSurface.SizeChangedEvent:
					Control.SizeChanged += (sender, e) => Callback.OnResize(Widget, new ResizeEventArgs(Control.Size));
					break;
				default:
					base.AttachEvent(id);
					break;
			}
		}

		public virtual void InitializeGraphicsApi()
		{
			if (!Widget.ControlReady)
			{
				return;
			}

			switch (Widget.GLReady)
			{
				case false:
					return;
				case true:
					InitializeOpenGL();
					break;
				case null:
					InitializeOtherApi();
					break;
			}

			// Ideally Callback.OnVeldridInitialized would be called here, but
			// WPF needs to delay raising that event until after WpfVeldridHost
			// has been Loaded. Each platform's XVeldridSurfaceHandler therefore
			// has to call OnVeldridInitialized itself.
		}

		/// <summary>
		/// Prepare this VeldridSurface to use OpenGL.
		/// </summary>
		/// <remarks>
		/// OpenGL initialization is platform-dependent, but here it happens by
		/// way of GLSurface, which for users of the class is cross-platform.
		/// </remarks>
		protected virtual void InitializeOpenGL()
		{
			(RenderTarget as GLSurface).MakeCurrent();

			VeldridGL.Surfaces.Add(RenderTarget as GLSurface);

			var platformInfo = new OpenGLPlatformInfo(
				VeldridGL.GetGLContextHandle(),
				VeldridGL.GetProcAddress,
				VeldridGL.MakeCurrent,
				VeldridGL.GetCurrentContext,
				VeldridGL.ClearCurrentContext,
				VeldridGL.DeleteContext,
				VeldridGL.SwapBuffers,
				VeldridGL.SetVSync,
				VeldridGL.SetSwapchainFramebuffer,
				VeldridGL.ResizeSwapchain);

			Widget.GraphicsDevice = GraphicsDevice.CreateOpenGL(
				new GraphicsDeviceOptions(false, Veldrid.PixelFormat.R32_Float, false),
				platformInfo,
				(uint)Widget.Width,
				(uint)Widget.Height);

			Widget.Swapchain = Widget.GraphicsDevice.MainSwapchain;

			Callback.OnVeldridInitialized(Widget, EventArgs.Empty);
		}

		/// <summary>
		/// Prepare this VeldridSurface to use a graphics API other than OpenGL.
		/// </summary>
		protected virtual void InitializeOtherApi()
		{
			if (Widget.Backend == GraphicsBackend.Metal)
			{
				Widget.GraphicsDevice = GraphicsDevice.CreateMetal(new GraphicsDeviceOptions());
			}
			else if (Widget.Backend == GraphicsBackend.Vulkan)
			{
				Widget.GraphicsDevice = GraphicsDevice.CreateVulkan(new GraphicsDeviceOptions());
			}
			else if (Widget.Backend == GraphicsBackend.Direct3D11)
			{
				Widget.GraphicsDevice = GraphicsDevice.CreateD3D11(new GraphicsDeviceOptions());
			}
			else
			{
				string message;
				if (!Enum.IsDefined(typeof(GraphicsBackend), Widget.Backend))
				{
					message = "Unrecognized backend!";
				}
				else
				{
					message = "Specified backend not supported on this platform!";
				}

				throw new ArgumentException(message);
			}
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
			Control RenderTarget { get; set; }
			void InitializeGraphicsApi();
		}

		public new IHandler Handler => (IHandler)base.Handler;

		public new interface ICallback : Control.ICallback
		{
			void OnDraw(VeldridSurface s, EventArgs e);
			void OnResize(VeldridSurface s, ResizeEventArgs e);
			void OnVeldridInitialized(VeldridSurface s, EventArgs e);
		}

		protected new class Callback : Control.Callback, ICallback
		{
			public void OnDraw(VeldridSurface s, EventArgs e)
			{
				s.OnDraw(e);
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

		public static GraphicsBackend PreferredBackend
		{
			get
			{
				GraphicsBackend backend;
				if (GraphicsDevice.IsBackendSupported(GraphicsBackend.Metal))
				{
					backend = GraphicsBackend.Metal;
				}
				else if (GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan))
				{
					backend = GraphicsBackend.Vulkan;
				}
				else if (GraphicsDevice.IsBackendSupported(GraphicsBackend.Direct3D11))
				{
					backend = GraphicsBackend.Direct3D11;
				}
				else
				{
					backend = GraphicsBackend.OpenGL;
				}

				return backend;
			}
		}

		public GraphicsBackend Backend { get; set; }

		public GraphicsDevice GraphicsDevice { get; set; }
		public Swapchain Swapchain { get; set; }

		private bool? _glReady = null;
		public bool? GLReady
		{
			get { return _glReady; }
			private set
			{
				_glReady = value;

				Handler.InitializeGraphicsApi();
			}
		}

		private bool _controlReady = false;
		public bool ControlReady
		{
			get { return _controlReady; }
			private set
			{
				_controlReady = value;

				Handler.InitializeGraphicsApi();
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
				GLReady = false;

				// Remember to match these graphics mode settings to whatever's
				// used in VeldridBackEnd.CreatePipeline and in the instance of
				// XVeldridSurfaceHandler for a given platform.
				var mode = new GraphicsMode(new ColorFormat(32), 32);
				int major = 3;
				int minor = 3;
				GraphicsContextFlags flags = GraphicsContextFlags.ForwardCompatible;

				var surface = new GLSurface(mode, major, minor, flags);
				surface.GLInitalized += (sender, e) => GLReady = true;
				surface.Draw += (sender, e) => OnDraw(EventArgs.Empty);

				Handler.RenderTarget = surface;
			}

			LoadComplete += (sender, e) => ControlReady = true;
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

			OnResize(new ResizeEventArgs(Width, Height));
		}
	}
}
