using Eto.Forms;
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

            Shown += MainForm_Shown;
            SizeChanged += MainForm_SizeChanged;

            Content = new VeldridSurface(initOther, backend);

            Driver.Surface = Content as VeldridSurface;
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            Driver.SetUpVeldrid();

            var s = Driver.Surface;
            s.Swapchain?.Resize((uint)s.Width, (uint)s.Height);

            Driver.Draw();
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            var s = Driver.Surface;
            s.Swapchain?.Resize((uint)s.Width, (uint)s.Height);

            Driver.Draw();
        }
    }
}
