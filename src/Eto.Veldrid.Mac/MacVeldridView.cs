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

		public IWindowInfo WindowInfo { get; protected set; }

		public event EventHandler Draw;
		public event EventHandler WindowInfoUpdated;

		public IWindowInfo UpdateWindowInfo(GraphicsMode mode) => UpdateWindowInfo();
		public IWindowInfo UpdateWindowInfo()
		{
			WindowInfo?.Dispose();

			WindowInfo = Utilities.CreateMacOSWindowInfo(Window.Handle, Handle);

			WindowInfoUpdated?.Invoke(this, EventArgs.Empty);

			return WindowInfo;
		}

		public override void DidChangeBackingProperties()
		{
			base.DidChangeBackingProperties();

			// If backing properties have changed, but WindowInfo hasn't been
			// assigned a value yet, trying to update it will silently cause
			// rendering errors, in particular a blank window.
			if (WindowInfo == null)
			{
				return;
			}

			UpdateWindowInfo();
		}

		public override void DrawRect(CGRect dirtyRect)
		{
			Draw?.Invoke(this, EventArgs.Empty);
		}
	}
}
