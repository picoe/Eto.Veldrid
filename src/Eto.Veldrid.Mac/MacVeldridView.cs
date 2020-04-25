using Eto.Mac.Forms;
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

		public event EventHandler Draw;

		public override void DrawRect(CGRect dirtyRect)
		{
			Draw?.Invoke(this, EventArgs.Empty);
		}
	}
}
