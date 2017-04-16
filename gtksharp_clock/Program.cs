using System;
using Gtk;

// http://www.mono-project.com/docs/gui/gtksharp/widgets/widget-colours/

namespace gtksharp_clock
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Application.Init();
			ClockWindow win = new ClockWindow ();
			win.Show();
			Application.Run();
		}
	}

	class ClockWindow : Window
	{
		public ClockWindow() : base("ClockWindow")
		{
			SetDefaultSize(250, 200);
			SetPosition(WindowPosition.Center);

			ClockFace cf = new ClockFace();

			Gdk.Color black = new Gdk.Color();
			Gdk.Color.Parse("black", ref black);
			Gdk.Color grey = new Gdk.Color();
			Gdk.Color.Parse("grey", ref grey);

			this.ModifyBg(StateType.Normal, grey);
			cf.ModifyBg(StateType.Normal, grey);

			this.ModifyFg(StateType.Normal, black);
			cf.ModifyFg(StateType.Normal, black);

			this.DeleteEvent += DeleteWindow;

			Add(cf);
			ShowAll();
		}

		static void DeleteWindow(object obj, DeleteEventArgs args)
		{
			Application.Quit();
		}
	}

	class ClockFace : DrawingArea
	{

		public ClockFace() : base()
		{
			this.SetSizeRequest(600, 600);
			this.ExposeEvent += OnExposed;
		}

		public void OnExposed(object o, ExposeEventArgs args)
		{
			
			Gdk.Color black = new Gdk.Color();
			Gdk.Color.Parse("black", ref black);

			this.ModifyFg(StateType.Normal, black);

			this.GdkWindow.DrawLine(this.Style.BaseGC(StateType.Normal), 0, 0, 400, 300);
		}
	}
}
