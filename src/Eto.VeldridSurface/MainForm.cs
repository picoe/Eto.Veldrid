using Eto.Forms;
using Eto.Gl;
using System;
using Veldrid;

namespace Eto.VeldridSurface
{
	public partial class MainForm : Form
	{
		VeldridSurface Surface;

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

		public MainForm(Action<VeldridSurface, GraphicsBackend, Action> initOther, GraphicsBackend backend)
		{
			InitializeComponent();

			Shown += (sender, e) => FormReady = true;

			Surface = new VeldridSurface(initOther, backend);

			if (Surface.Content is GLSurface g)
			{
				g.Draw += (sender, e) => Surface.Driver.Draw();
				g.SizeChanged += (sender, e) =>
				{
					VeldridSurface s = Surface.Driver.Surface;
					s?.Swapchain?.Resize((uint)s.Width, (uint)s.Height);
				};
			}
			else
			{
				Surface.SizeChanged += Surface_SizeChanged;
			}

			Content = Surface;

			Surface.VeldridSurfaceInitialized += (sender, e) => VeldridReady = true;
		}

		private void SetUpVeldrid(bool formReady, bool veldridReady)
		{
			if (!(formReady && VeldridReady))
			{
				return;
			}

			Surface.Driver.SetUpVeldrid();

			Surface?.Swapchain?.Resize((uint)Surface.Width, (uint)Surface.Height);

			Surface.Driver.Clock.Start();
		}

		private void Surface_SizeChanged(object sender, EventArgs e)
		{
			Surface?.Swapchain?.Resize((uint)Surface.Width, (uint)Surface.Height);

			Surface.Driver.Draw();
		}
	}
}
