using Eto.Drawing;
using Eto.Forms;
using Eto.Gl;
using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Reflection;
using Veldrid;

namespace Eto.VeldridSurface
{
	public static class VeldridGL
	{
		public static Dictionary<IntPtr, GLSurface> Contexts = new Dictionary<IntPtr, GLSurface>();

		public static List<GLSurface> Surfaces = new List<GLSurface>();

		public static IntPtr GetGLContextHandle()
		{
			return GetCurrentContext();
		}

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
			GraphicsBackend Backend { get; }
			GraphicsDevice GraphicsDevice { get; set; }
			Swapchain Swapchain { get; set; }

			void OnDraw(VeldridSurface s, EventArgs e);
			void OnResize(VeldridSurface s, ResizeEventArgs e);
		}

		protected new class Callback : Control.Callback, ICallback
		{
			public Func<GraphicsBackend> GetBackend { get; set; }
			public GraphicsBackend Backend => GetBackend.Invoke();

			public Func<GraphicsDevice> GetGraphicsDevice { get; set; }
			public Action<GraphicsDevice> SetGraphicsDevice { get; set; }
			public GraphicsDevice GraphicsDevice
			{
				get { return GetGraphicsDevice(); }
				set { SetGraphicsDevice(value); }
			}

			public Func<Swapchain> GetSwapchain { get; set; }
			public Action<Swapchain> SetSwapchain { get; set; }
			public Swapchain Swapchain
			{
				get { return GetSwapchain(); }
				set { SetSwapchain(value); }
			}

			public void OnDraw(VeldridSurface s, EventArgs e)
			{
				s.OnDraw(e);
			}

			public void OnResize(VeldridSurface s, ResizeEventArgs e)
			{
				s.OnResize(e);
			}
		}

		protected override object GetCallback()
		{
			return new Callback
			{
				GetBackend = () => { return Backend; },
				GetGraphicsDevice = () => { return GraphicsDevice; },
				SetGraphicsDevice = (d) => { GraphicsDevice = d; },
				GetSwapchain = () => { return Swapchain; },
				SetSwapchain = (s) => { Swapchain = s; }
			};
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

				RaiseInitEventIfReady();
			}
		}

		private bool _controlReady = false;
		public bool ControlReady
		{
			get { return _controlReady; }
			private set
			{
				_controlReady = value;

				RaiseInitEventIfReady();
			}
		}

		public static string VeldridInitializedEvent = "VeldridSurface.VeldridInitialized";
		public static string DrawEvent = "VeldridSurface.Draw";
		public static string ResizeEvent = "VeldridSurface.Resize";

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

				var surface = new GLSurface(new GraphicsMode(), 3, 3, GraphicsContextFlags.ForwardCompatible);
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

		private void InitGL()
		{
			VeldridGL.Surfaces.Add(Handler.RenderTarget as GLSurface);

			var platformInfo = new Veldrid.OpenGL.OpenGLPlatformInfo(
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

			GraphicsDevice = GraphicsDevice.CreateOpenGL(
				new GraphicsDeviceOptions(),
				platformInfo,
				640,
				480);

			Swapchain = GraphicsDevice.MainSwapchain;
		}

		private void RaiseInitEventIfReady()
		{
			if (!ControlReady)
			{
				return;
			}

			switch (GLReady)
			{
				case false:
					return;
				case true:
					(Handler.RenderTarget as GLSurface).MakeCurrent();
					InitGL();
					break;
				case null:
					Handler.InitializeGraphicsApi();
					break;
			}

			OnVeldridInitialized(EventArgs.Empty);
		}
	}
}
