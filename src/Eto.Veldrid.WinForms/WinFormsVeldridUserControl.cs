using OpenTK.Graphics;
using OpenTK.Platform;
using System;
using System.Windows.Forms;

namespace Eto.Veldrid.WinForms
{
	public class WinFormsVeldridUserControl : UserControl
	{
		public IWindowInfo WindowInfo { get; protected set; }

		public event EventHandler WindowInfoUpdated;

		protected override CreateParams CreateParams
		{
			get
			{
				const int
					CS_VREDRAW = 0x01,
					CS_HREDRAW = 0x02,
					CS_OWNDC = 0x20;

				CreateParams cp = base.CreateParams;

				if (OpenTK.Configuration.RunningOnWindows)
				{
					// Necessary for OpenGL in Windows.
					cp.ClassStyle |= CS_VREDRAW | CS_HREDRAW | CS_OWNDC;
				}

				return cp;
			}
		}

		public WinFormsVeldridUserControl()
		{
			SetStyle(ControlStyles.Opaque, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			DoubleBuffered = false;

			BackColor = System.Drawing.Color.HotPink;
		}

		public IWindowInfo UpdateWindowInfo(GraphicsMode mode)
		{
			WindowInfo?.Dispose();

			WindowInfo = Utilities.CreateWindowsWindowInfo(Handle);

			WindowInfoUpdated?.Invoke(this, EventArgs.Empty);

			return WindowInfo;
		}
	}
}
