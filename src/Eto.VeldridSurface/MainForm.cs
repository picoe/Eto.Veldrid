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

				SetUpVeldrid();
			}
		}

		private bool _formReady = false;
		public bool FormReady
		{
			get { return _formReady; }
			set
			{
				_formReady = value;

				SetUpVeldrid();
			}
		}

		public MainForm(GraphicsBackend backend)
		{
			InitializeComponent();

			Shown += (sender, e) => FormReady = true;

			Surface = new VeldridSurface(backend);
			Surface.VeldridSurfaceInitialized += (sender, e) => VeldridReady = true;

			Content = Surface;
		}

		private void SetUpVeldrid()
		{
			if (!(FormReady && VeldridReady))
			{
				return;
			}

			Surface.Driver.SetUpVeldrid();

			Surface.Resize(Surface.Width, Surface.Height);

			Surface.Driver.Clock.Start();
		}
	}
}
