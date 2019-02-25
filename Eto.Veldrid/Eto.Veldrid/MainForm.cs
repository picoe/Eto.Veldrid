using Eto.Forms;
using Eto.Gl;
using System;
using Veldrid;

namespace Eto.VeldridSurface
{
	public partial class MainForm : Form
	{
		public VeldridDriver Driver { get; } = new VeldridDriver();

		public MainForm(Action<VeldridSurface, GraphicsBackend> initOther, GraphicsBackend backend)
		{
			InitializeComponent();

			var surface = new VeldridSurface(initOther, backend);

			if (surface.Content is GLSurface g)
			{
				g.Draw += (sender, e) => Driver.Draw();
				g.SizeChanged += (sender, e) =>
				{
					VeldridSurface s = Driver.Surface;
					s?.Swapchain?.Resize((uint)s.Width, (uint)s.Height);
				};
			}
			else
			{
				surface.SizeChanged += Surface_SizeChanged;
			}

			Content = surface;

			Driver.Surface = surface;

			surface.VeldridSurfaceInitialized += Surface_VeldridSurfaceInitialized;
		}

		private void Surface_SizeChanged(object sender, EventArgs e)
		{
			VeldridSurface s = Driver.Surface;
			s?.Swapchain?.Resize((uint)s.Width, (uint)s.Height);

			Driver.Draw();
		}

		private void Surface_VeldridSurfaceInitialized(object sender, EventArgs e)
		{
			Driver.SetUpVeldrid();

			VeldridSurface s = Driver.Surface;
			s?.Swapchain?.Resize((uint)s.Width, (uint)s.Height);

			Driver.Draw();
		}
	}
}
