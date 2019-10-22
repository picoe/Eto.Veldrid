using Eto.Mac.Forms;
using OpenTK.Graphics;
using OpenTK.Platform;
using System;

#if MONOMAC
using MonoMac.AppKit;
using MonoMac.CoreGraphics;
#elif XAMMAC2
using AppKit;
using CoreGraphics;
#endif

namespace Eto.Veldrid.Mac
{
	public class MacVeldridView : NSView, IMacControl
	{
		public override bool AcceptsFirstMouse(NSEvent theEvent) => CanFocus;

		public override bool AcceptsFirstResponder() => CanFocus;

		public bool CanFocus { get; set; } = true;

		public WeakReference WeakHandler { get; set; }

		GraphicsMode Mode = new GraphicsMode(new ColorFormat(32), 8, 8);

		public IWindowInfo WindowInfo { get; set; }
		public GraphicsContext Context { get; private set; }

		public event EventHandler Draw;

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
}
