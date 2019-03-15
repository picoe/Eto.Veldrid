using Eto.Forms;
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

		public MainForm(Action<VeldridSurface, GraphicsBackend, Action, Action<int, int>> initOther, GraphicsBackend backend)
		{
			InitializeComponent();

			Shown += (sender, e) => FormReady = true;

			Surface = new VeldridSurface(initOther, backend);
			Surface.VeldridSurfaceInitialized += (sender, e) => VeldridReady = true;

			Content = Surface;
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
	}
}
