using Eto.Forms;
using System;
using Veldrid;

namespace PlaceholderName
{
	public partial class MainForm : Form
	{
		VeldridSurface Surface;

		VeldridDriver Driver;

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

		public MainForm(GraphicsBackend backend) : this(backend, "shaders")
		{
		}
		public MainForm(GraphicsBackend backend, string shaderSubdirectory)
		{
			InitializeComponent();

			Shown += (sender, e) => FormReady = true;

			Surface = new VeldridSurface(backend);
			Surface.VeldridInitialized += (sender, e) => VeldridReady = true;

			Content = Surface;

			Driver = new VeldridDriver { Surface = Surface, ShaderSubdirectory = shaderSubdirectory };

			// TODO: Make this binding actually work both ways.
			CmdAnimate.Bind<bool>("Checked", Driver, "Animate");
			CmdClockwise.Bind<bool>("Checked", Driver, "Clockwise");
		}

		private void SetUpVeldrid()
		{
			if (!(FormReady && VeldridReady))
			{
				return;
			}

			Driver.SetUpVeldrid();

			Title = $"Veldrid backend: {Surface.Backend.ToString()}";

			Driver.Clock.Start();
		}
	}
}
