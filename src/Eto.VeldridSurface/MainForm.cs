using Eto.Forms;
using Eto.Gl;
using System;
using Veldrid;

namespace Eto.VeldridSurface
{
	public partial class MainForm : Form
	{
		public VeldridDriver Driver { get; } = new VeldridDriver();

		private bool _veldridReady = false;
		public bool VeldridReady
		{
			get { return _veldridReady; }
			private set
			{
				_veldridReady = value;

				SetUpVeldrid(FormReady, VeldridReady);
			}
		}

		private bool _formReady = false;
		public bool FormReady
		{
			get { return _formReady; }
			set
			{
				_formReady = value;

				SetUpVeldrid(FormReady, VeldridReady);
			}
		}

		public MainForm(Action<VeldridSurface, GraphicsBackend> initOther, GraphicsBackend backend)
		{
			InitializeComponent();

			Shown += (sender, e) => FormReady = true;

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

			surface.VeldridSurfaceInitialized += (sender, e) => VeldridReady = true;
		}

		private void SetUpVeldrid(bool formReady, bool veldridReady)
		{
			if (!(formReady && VeldridReady))
			{
				return;
			}

			Driver.SetUpVeldrid();

			VeldridSurface s = Driver.Surface;
			s?.Swapchain?.Resize((uint)s.Width, (uint)s.Height);

			Driver.Draw();
		}

		private void Surface_SizeChanged(object sender, EventArgs e)
		{
			VeldridSurface s = Driver.Surface;
			s?.Swapchain?.Resize((uint)s.Width, (uint)s.Height);

			Driver.Draw();
		}
	}
}
