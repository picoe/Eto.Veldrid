using Eto.Drawing;
using Eto.Forms;
using System;
using System.Windows.Forms.Integration;
using swf = System.Windows.Forms;
using swi = System.Windows.Input;

namespace Eto.Wpf.Forms
{
	/// <summary>
	/// An experimental imitation of Eto.Wpf.Forms.WindowsFormsHostHandler that
	/// manually raises some WPF events to hopefully make them bubble properly.
	/// </summary>
	public class ManualBubbleWindowsFormsHostHandler<TControl, TWidget, TCallback> : WpfFrameworkElement<WindowsFormsHost, TWidget, TCallback>
			where TControl : swf.Control
			where TWidget : Control
			where TCallback : Control.ICallback
	{
		public override bool UseKeyPreview => false;
		public override bool UseMousePreview => false;

		public override Color BackgroundColor
		{
			get { return WinFormsControl.BackColor.ToEto(); }
			set { WinFormsControl.BackColor = value.ToSD(); }
		}

		public TControl WinFormsControl
		{
			get { return (TControl)Control.Child; }
			set { Control.Child = value; }
		}

		public ManualBubbleWindowsFormsHostHandler(TControl control)
			: this()
		{
			Control.Child = control;
		}

		public ManualBubbleWindowsFormsHostHandler() : base()
		{
			Control = new WindowsFormsHost();
		}

		public override void AttachEvent(string id)
		{
			switch (id)
			{
				case Eto.Forms.Control.MouseMoveEvent:
					WinFormsControl.MouseMove += WinFormsControl_MouseMove;
					base.AttachEvent(id);
					break;
				case Eto.Forms.Control.MouseUpEvent:
					WinFormsControl.MouseUp += WinFormsControl_MouseUp;
					base.AttachEvent(id);
					break;
				case Eto.Forms.Control.MouseDownEvent:
					WinFormsControl.MouseDown += WinFormsControl_MouseDown;
					base.AttachEvent(id);
					break;
				case Eto.Forms.Control.MouseDoubleClickEvent:
					WinFormsControl.MouseDoubleClick += WinFormsControl_MouseDoubleClick;
					break;
				case Eto.Forms.Control.MouseEnterEvent:
					WinFormsControl.MouseEnter += WinFormsControl_MouseEnter;
					break;
				case Eto.Forms.Control.MouseLeaveEvent:
					WinFormsControl.MouseLeave += WinFormsControl_MouseLeave;
					break;
				case Eto.Forms.Control.MouseWheelEvent:
					Control.MouseWheel += Control_MouseWheel;
					WinFormsControl.MouseWheel += WinFormsControl_MouseWheel;
					break;
				case Eto.Forms.Control.KeyDownEvent:
					WinFormsControl.KeyDown += WinFormsControl_KeyDown;
					WinFormsControl.KeyPress += WinFormsControl_KeyPress;
					break;
				case Eto.Forms.Control.KeyUpEvent:
					WinFormsControl.KeyUp += WinFormsControl_KeyUp;
					break;
				case TextControl.TextChangedEvent:
					WinFormsControl.TextChanged += WinFormsControl_TextChanged;
					break;
				case Eto.Forms.Control.TextInputEvent:
					HandleEvent(Eto.Forms.Control.KeyDownEvent);
					break;
				case Eto.Forms.Control.GotFocusEvent:
					WinFormsControl.GotFocus += WinFormsControl_GotFocus;
					break;
				case Eto.Forms.Control.LostFocusEvent:
					WinFormsControl.LostFocus += WinFormsControl_LostFocus;
					break;
				default:
					base.AttachEvent(id);
					break;
			}
		}

		private void Control_MouseWheel(object sender, swi.MouseWheelEventArgs e)
		{
			// To ensure all involved overrides are called in the right order,
			// call the callback, consider the event handled, and then tell WPF
			// to continue on its merry way with the Preview version instead.

			Callback.OnMouseWheel(Widget, e.ToEto(Control));

			e.Handled = true;

			var args = new swi.MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
			{
				RoutedEvent = swi.Mouse.PreviewMouseWheelEvent,
				Source = e.Source
			};

			Control.RaiseEvent(args);
		}

		Keys key;
		bool handled;
		char keyChar;
		bool charPressed;
		public Keys? LastKeyDown { get; set; }

		void WinFormsControl_KeyDown(object sender, swf.KeyEventArgs e)
		{
			charPressed = false;
			handled = true;
			key = e.KeyData.ToEto();

			if (key != Keys.None && LastKeyDown != key)
			{
				var kpea = new KeyEventArgs(key, KeyEventType.KeyDown);
				Callback.OnKeyDown(Widget, kpea);

				handled = e.SuppressKeyPress = e.Handled = kpea.Handled;
			}
			else
				handled = false;

			if (!handled && charPressed)
			{
				// this is when something in the event causes messages to be processed for some reason (e.g. show dialog box)
				// we want the char event to come after the dialog is closed, and handled is set to true!
				var kpea = new KeyEventArgs(key, KeyEventType.KeyDown, keyChar);
				Callback.OnKeyDown(Widget, kpea);
				e.SuppressKeyPress = e.Handled = kpea.Handled;
			}

			LastKeyDown = null;
		}

		void WinFormsControl_KeyPress(object sender, swf.KeyPressEventArgs e)
		{
			charPressed = true;
			keyChar = e.KeyChar;
			if (!handled)
			{
				if (!char.IsControl(e.KeyChar))
				{
					var tia = new TextInputEventArgs(keyChar.ToString());
					Callback.OnTextInput(Widget, tia);
					e.Handled = tia.Cancel;
				}

				if (!e.Handled)
				{
					var kpea = new KeyEventArgs(key, KeyEventType.KeyDown, keyChar);
					Callback.OnKeyDown(Widget, kpea);
					e.Handled = kpea.Handled;
				}
			}
			else
				e.Handled = true;
		}

		void WinFormsControl_KeyUp(object sender, swf.KeyEventArgs e)
		{
			key = e.KeyData.ToEto();

			var kpea = new KeyEventArgs(key, KeyEventType.KeyUp);
			Callback.OnKeyUp(Widget, kpea);
			e.Handled = kpea.Handled;
		}

		void WinFormsControl_TextChanged(object sender, EventArgs e)
		{
			var widget = Widget as TextControl;
			if (widget != null)
			{
				var callback = (TextControl.ICallback)((ICallbackSource)widget).Callback;
				callback.OnTextChanged(widget, e);
			}
		}

		void WinFormsControl_MouseWheel(object sender, swf.MouseEventArgs e)
		{
			var args = new swi.MouseWheelEventArgs(swi.InputManager.Current.PrimaryMouseDevice, Environment.TickCount, e.Delta)
			{
				RoutedEvent = swi.Mouse.MouseWheelEvent,
				Source = Control
			};

			Control.RaiseEvent(args);
		}

		void WinFormsControl_MouseLeave(object sender, EventArgs e) => Callback.OnMouseLeave(Widget, new MouseEventArgs(Mouse.Buttons, Keyboard.Modifiers, PointFromScreen(Mouse.Position)));

		void WinFormsControl_MouseEnter(object sender, EventArgs e) => Callback.OnMouseEnter(Widget, new MouseEventArgs(Mouse.Buttons, Keyboard.Modifiers, PointFromScreen(Mouse.Position)));

		void WinFormsControl_MouseDoubleClick(object sender, swf.MouseEventArgs e) => Callback.OnMouseDoubleClick(Widget, e.ToEto(WinFormsControl));

		void WinFormsControl_MouseDown(object sender, swf.MouseEventArgs e)
		{
			// Contrary to most WPF controls, the WindowsFormsHost class seems
			// to prevent correct mouse event data from being obtained (e.g.
			// which buttons were pressed, and at what location). The solution
			// is capturing the mouse long enough to build args...
			Control.CaptureMouse();

			MouseEventArgs eto = e.ToEto(WinFormsControl);

			swi.MouseButton changed = eto.ToWpf().ChangedButton;
			var args = new swi.MouseButtonEventArgs(swi.InputManager.Current.PrimaryMouseDevice, Environment.TickCount, changed)
			{
				RoutedEvent = swi.Mouse.MouseDownEvent,
				Source = Control
			};

			// ...but releasing it before continuing, in case the mouse event in
			// question is one that shouldn't hold onto the mouse.
			Control.ReleaseMouseCapture();

			Control.RaiseEvent(args);
		}

		void WinFormsControl_MouseUp(object sender, swf.MouseEventArgs e)
		{
			Control.CaptureMouse();

			MouseEventArgs eto = e.ToEto(WinFormsControl);

			swi.MouseButton changed = eto.ToWpf().ChangedButton;
			var args = new swi.MouseButtonEventArgs(swi.InputManager.Current.PrimaryMouseDevice, Environment.TickCount, changed)
			{
				RoutedEvent = swi.Mouse.MouseUpEvent,
				Source = Control
			};

			Control.ReleaseMouseCapture();

			Control.RaiseEvent(args);
		}

		void WinFormsControl_MouseMove(object sender, swf.MouseEventArgs e)
		{
			var args = new swi.MouseEventArgs(swi.InputManager.Current.PrimaryMouseDevice, Environment.TickCount)
			{
				RoutedEvent = swi.Mouse.MouseMoveEvent,
				Source = Control
			};

			Control.RaiseEvent(args);
		}

		void WinFormsControl_LostFocus(object sender, EventArgs e) => Callback.OnLostFocus(Widget, EventArgs.Empty);

		void WinFormsControl_GotFocus(object sender, EventArgs e) => Callback.OnGotFocus(Widget, EventArgs.Empty);

		public override void Focus()
		{
			if (Widget.Loaded && WinFormsControl.IsHandleCreated)
				WinFormsControl.Focus();
			else
				Widget.LoadComplete += Widget_LoadComplete;
		}

		void Widget_LoadComplete(object sender, EventArgs e)
		{
			Widget.LoadComplete -= Widget_LoadComplete;
			WinFormsControl.Focus();
		}

		public override bool HasFocus => WinFormsControl.Focused;

		public override bool AllowDrop
		{
			get { return WinFormsControl.AllowDrop; }
			set { WinFormsControl.AllowDrop = value; }
		}

		public override void SuspendLayout()
		{
			base.SuspendLayout();
			WinFormsControl.SuspendLayout();
		}

		public override void ResumeLayout()
		{
			base.ResumeLayout();
			WinFormsControl.ResumeLayout();
		}

		public override void Invalidate(bool invalidateChildren)
		{
			WinFormsControl.Invalidate(invalidateChildren);
		}

		public override void Invalidate(Rectangle rect, bool invalidateChildren)
		{
			WinFormsControl.Invalidate(rect.ToSD(), invalidateChildren);
		}

		public override bool Enabled
		{
			get { return WinFormsControl.Enabled; }
			set { WinFormsControl.Enabled = value; }
		}
	}
}
