using Eto.Forms;
using System;
using swf = System.Windows.Forms;
using swi = System.Windows.Input;

namespace Eto.Wpf.Forms
{
	public static class WpfVeldridExtensions
	{
		public static MouseButtons ToEto(this swi.MouseButton wpfButton)
		{
			MouseButtons etoButton = MouseButtons.None;

			// The System.Windows.Input.MouseButton type isn't a Flags enum, so
			// etoButton doesn't benefit from appending with bitwise OR.
			if (wpfButton == swi.MouseButton.Left)
			{
				etoButton = MouseButtons.Primary;
			}
			else if (wpfButton == swi.MouseButton.Middle)
			{
				etoButton = MouseButtons.Middle;
			}
			else if (wpfButton == swi.MouseButton.Right)
			{
				etoButton = MouseButtons.Alternate;
			}

			return etoButton;
		}

		public static swi.MouseButtonEventArgs ToWpf(this MouseEventArgs e)
		{
			swi.MouseButton button = 0;

			if (e.Buttons.HasFlag(MouseButtons.Primary))
			{
				button |= swi.MouseButton.Left;
			}
			if (e.Buttons.HasFlag(MouseButtons.Middle))
			{
				button |= swi.MouseButton.Middle;
			}
			if (e.Buttons.HasFlag(MouseButtons.Alternate))
			{
				button |= swi.MouseButton.Right;
			}

			return new swi.MouseButtonEventArgs(swi.InputManager.Current.PrimaryMouseDevice, Environment.TickCount, button);
		}

		public static swi.MouseButton? ToWpf(this swf.MouseButtons winformsButton)
		{
			swi.MouseButton? wpfButton;

			// According to the System.Windows.Forms.MouseEventArgs.Button docs,
			// it represents "one of the System.Windows.Forms.MouseButtons
			// values" despite said enum having the Flags attribute. This switch
			// should then be safe, since the input is only ever one button.
			switch (winformsButton)
			{
				case swf.MouseButtons.Left:
					wpfButton = swi.MouseButton.Left;
					break;
				case swf.MouseButtons.None:
					wpfButton = null;
					break;
				case swf.MouseButtons.Right:
					wpfButton = swi.MouseButton.Right;
					break;
				case swf.MouseButtons.Middle:
					wpfButton = swi.MouseButton.Middle;
					break;
				case swf.MouseButtons.XButton1:
					wpfButton = swi.MouseButton.XButton1;
					break;
				case swf.MouseButtons.XButton2:
					wpfButton = swi.MouseButton.XButton2;
					break;
				default:
					throw new ArgumentOutOfRangeException("winformsButton");
			}

			return wpfButton;
		}
	}
}
