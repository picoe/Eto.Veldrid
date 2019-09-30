using Eto.Drawing;
using Eto.Forms;

namespace VeldridEto
{
	public partial class MainForm : Form
	{
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
				AboutItem = aboutCommand
			};
		}
	}
}
