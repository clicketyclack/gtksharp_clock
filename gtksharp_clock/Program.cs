using System;
using Gtk;
using NUnit.Framework;

// http://www.mono-project.com/docs/gui/gtksharp/widgets/widget-colours/

namespace gtksharp_clock
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Application.Init();
			ClockWindow win = new ClockWindow();
			win.Show();
			Application.Run();
		}
	}

	class ClockColors
	{
		/// <summary>
		/// Singleton. See
		/// https://msdn.microsoft.com/en-us/library/ms998558.aspx
		/// </summary>
		private static ClockColors instance;

		public readonly Gdk.Color black = new Gdk.Color();
		public readonly Gdk.Color grey = new Gdk.Color();
		public readonly Gdk.Color blue = new Gdk.Color();

		private ClockColors() {
			// ClockColors.black
			Gdk.Color.Parse("black", ref black);
			//ClockColors.grey = new Gdk.Color();
			Gdk.Color.Parse("grey", ref grey);
			//ClockColors.blue = new Gdk.Color();
			Gdk.Color.Parse("blue", ref blue);
		}

		public static ClockColors Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new ClockColors();
				}
				return instance;
			}
		}
	}


	class ClockWindow : Window
	{

		private System.Timers.Timer timer;

		public ClockWindow() : base("ClockWindow")
		{
			SetDefaultSize(250, 200);
			SetPosition(WindowPosition.Center);

			ClockFace cf = new ClockFace();
			ClockColors colors = ClockColors.Instance;

			this.ModifyBg(StateType.Normal, colors.grey);
			cf.ModifyBg(StateType.Normal, colors.grey);

			this.ModifyFg(StateType.Normal, colors.black);
			cf.ModifyFg(StateType.Normal, colors.black);
			this.DeleteEvent += DeleteWindow;

			Add(cf);
			ShowAll();

            this.timer = new System.Timers.Timer();
			this.timer.Interval = 10;
			this.timer.Elapsed += QueueRedraw;
			this.timer.AutoReset = true;
			this.timer.Enabled = true;

		}

		private void QueueRedraw(object o, System.Timers.ElapsedEventArgs e)
		{
			this.QueueDraw();
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


		/// <summary>
		/// Get the coordinates of a clock face arm.
		/// </summary>
		/// <param name="direction">The current digit pointed to by the clock arm. Accepts floats.</param>
		/// <param name="wraparound">The max value of a clock arm. 12 for the hour hand, 60 for minutes and seconds. Other ranges are accepted as well, as long as they are greater than 0.</param>
		/// <param name="length">The length of the clock arm.</param>
		/// <returns>
		/// Two int:s in an array, the x & y coordinates from the origin.
		/// </returns>
		public int[] getArmEndCoords(double direction, double wraparound, double length)
		{
			if (wraparound <= 0.0)
			{
				string message = String.Format("Expected a range greater than 0.0, got {0}.", wraparound);
				throw new ArithmeticException(message);
			}

			// Get the coords for the arm end 
			int[] toreturn = new int[2];
			toreturn[0] = (int)Math.Round(length * Math.Sin(2.0 * Math.PI * direction / wraparound));
			toreturn[1] = (int)Math.Round(- length * Math.Cos(2.0 * Math.PI * direction / wraparound));
			return toreturn;
		}

		public void OnExposed(object o, ExposeEventArgs args)
		{
			this.drawArms();
		}


		public void drawArms()
		{

			DateTime now = DateTime.Now;
			ClockColors colors = ClockColors.Instance;

			Gdk.GC gc = this.Style.BaseGC(StateType.Normal);
			// gc.Foreground = blue; // Issue here?
			gc.RgbFgColor = colors.blue;
			gc.SetLineAttributes(3, Gdk.LineStyle.Solid, Gdk.CapStyle.Round, Gdk.JoinStyle.Round);

			int[] coords = this.getArmEndCoords(now.Second + now.Millisecond/1000, 60.0, 300);
			this.GdkWindow.DrawLine(this.Style.BaseGC(StateType.Normal), 300, 300, 300 + coords[0], 300 + coords[1]);

			coords = this.getArmEndCoords(now.Minute + now.Second / 60.0, 60.0, 200);
			this.GdkWindow.DrawLine(this.Style.BaseGC(StateType.Normal), 300, 300, 300 + coords[0], 300 + coords[1]);

			coords = this.getArmEndCoords(now.Hour + now.Minute / 60.0, 12.0, 130);
			this.GdkWindow.DrawLine(this.Style.BaseGC(StateType.Normal), 300, 300, 300 + coords[0], 300 + coords[1]);

		}
	}

	[TestFixture]
	public class ClockFaceTest
	{
		[Test]
		public void TestArmLength()
		{
			ClockFace cf = new ClockFace();
			int[] coords = new int[0];

			double[] values = new double[] { -200, 0, 11.11, 20000 };
			double[] maxes = new double[] { 20, 200 };
			double[] lengths = new double[] { 4, 222, -50 };

			foreach (double length in lengths)
			{
				foreach (double value in values)
				{
					foreach (double max in maxes)
					{
						coords = cf.getArmEndCoords(value, max, length);
						double hyp_actual = coords[0] * coords[0] + coords[1] * coords[1];


						double abs_len = Math.Abs(length);
						double lower_expected = (abs_len - 1) * (abs_len - 1);
						double upper_expected = (abs_len + 1) * (abs_len + 1);
						String message = String.Format("Failed reasonable hypothenuse interval {0} <= {1}, <= {2}", lower_expected * 0.99, hyp_actual, upper_expected);
						Assert.IsTrue(lower_expected <= hyp_actual && hyp_actual <= upper_expected, message);
					}
				}
			}
		}

		[Test]
		public void TestArmEnd()
		{
			ClockFace cf = new ClockFace();


			Assert.AreEqual(2, cf.getArmEndCoords(0.0, 60.0, 300.0).Length);

			// Hour hand for three o'clock
			int[] coords = cf.getArmEndCoords(3.0, 12.0, 300.0);
			Assert.IsTrue(299.0 <= coords[0] && coords[0] <= 300.0, "Actual coords are {0}, {1}", coords[0], coords[1]);


			// Hour hand for 7:30.  -> -71, 71
			coords = cf.getArmEndCoords(7.5, 12.0, 100.0);
			Assert.IsTrue(-73.0 <= coords[0] && coords[0] <= -70.0, "For 7:30 x, actual coords are {0}, {1}", coords[0], coords[1]);
			Assert.IsTrue(70.0 <= coords[1] && coords[1] <= 73.0, "For 7:30 y, actual coords are {0}, {1}", coords[0], coords[1]);
		}
	}

}
