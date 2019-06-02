using Eto.Drawing;
using Eto.Forms;

namespace PlaceholderName
{
	public partial class MainForm : Form
	{
		public CheckCommand CmdAnimate = new CheckCommand
		{
			MenuText = "Animate",
			Checked = true
		};
		public CheckCommand CmdClockwise = new CheckCommand
		{
			MenuText = "&Clockwise",
			Shortcut = Keys.C,
			Checked = true
		};

		private void InitializeComponent()
		{
			Title = "Veldrid in Eto";
			ClientSize = new Size(400, 350);

			var quitCommand = new Command { MenuText = "Quit", Shortcut = Application.Instance.CommonModifier | Keys.Q };
			quitCommand.Executed += (sender, e) => Application.Instance.Quit();

			var aboutCommand = new Command { MenuText = "About..." };
			aboutCommand.Executed += (sender, e) => new AboutDialog().ShowDialog(this);

			Menu = new MenuBar
			{
				QuitItem = quitCommand,
				AboutItem = aboutCommand,
				Items =
				{
					new ButtonMenuItem { Text = "&View", Items = { CmdAnimate, CmdClockwise } }
				}
			};
		}
	}
}
