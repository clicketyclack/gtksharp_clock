﻿using System;
using System.Timers;
using Gtk;
using System.Linq;
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

		// A single instance of color objects.
		public readonly Gdk.Color black = new Gdk.Color();
		public readonly Gdk.Color grey = new Gdk.Color();
		public readonly Gdk.Color blue = new Gdk.Color();
		public readonly Gdk.Color dark_grey = new Gdk.Color(0xA9, 0xA9, 0xA9);
		public readonly Gdk.Color white_smoke = new Gdk.Color(0xF5, 0xF5, 0xF5);

		private ClockColors()
		{
			Gdk.Color.Parse("black", ref black);
			Gdk.Color.Parse("grey", ref grey);
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


	class ClockTimer : System.Timers.Timer
	{
		private ClockWindow win;

		public ClockTimer(ClockWindow win) : base()
		{

			this.Interval = 10;
			this.win = win;
			this.Elapsed += QueueRedraw;
			this.AutoReset = true;
			this.Enabled = true;
		}

		private void QueueRedraw(object o, System.Timers.ElapsedEventArgs e)
		{
			this.win.QueueDraw();
		}
	}


	class ClockWindow : Window
	{

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

			ClockTimer ct = new ClockTimer(this);
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
			toreturn[1] = (int)Math.Round(-length * Math.Cos(2.0 * Math.PI * direction / wraparound));
			return toreturn;
		}

		public void OnExposed(object o, ExposeEventArgs args)
		{
			this.drawArms();
		}


		private void drawHoursArm(double current_hours)
		{
			ClockColors colors = ClockColors.Instance;
			Gdk.GC gc = this.Style.BaseGC(StateType.Normal);
			gc.SetLineAttributes(2, Gdk.LineStyle.Solid, Gdk.CapStyle.Butt, Gdk.JoinStyle.Miter);

			int[][] coords = {
				this.getArmEndCoords(current_hours, 12.0, -20),
                this.getArmEndCoords(current_hours - 4.0, 12.0, 20),
                this.getArmEndCoords(current_hours - 0.1, 12.0, 100),
                this.getArmEndCoords(current_hours + 0.1, 12.0, 100),
				this.getArmEndCoords(current_hours + 4.0, 12.0, 20),
                this.getArmEndCoords(current_hours, 12.0, -20)
			};

			var thepoints = from coord in coords
							select new Gdk.Point(coord[0] + 300, coord[1] + 300);
			
			gc.RgbFgColor = colors.dark_grey;
            this.GdkWindow.DrawPolygon(gc, true, thepoints.ToArray());

			gc.RgbFgColor = colors.white_smoke;
            this.GdkWindow.DrawPolygon(gc, false, thepoints.ToArray());
		}

		private void drawMinutesArm(double current_minutes)
		{

			ClockColors colors = ClockColors.Instance;
			Gdk.GC gc = this.Style.BaseGC(StateType.Normal);
			gc.RgbFgColor = colors.blue;
			int[] coords = this.getArmEndCoords(current_minutes, 60.0, 200);
			this.GdkWindow.DrawLine(this.Style.BaseGC(StateType.Normal), 300, 300, 300 + coords[0], 300 + coords[1]);
		}

		private void drawSecondsArm(double current_seconds)
		{
			ClockColors colors = ClockColors.Instance;
			Gdk.GC gc = this.Style.BaseGC(StateType.Normal);
			gc.RgbFgColor = colors.black;
			gc.SetLineAttributes(3, Gdk.LineStyle.Solid, Gdk.CapStyle.Round, Gdk.JoinStyle.Round);
			int[] coords = this.getArmEndCoords(current_seconds, 60.0, 200);
			this.GdkWindow.DrawLine(this.Style.BaseGC(StateType.Normal), 300, 300, 300 + coords[0], 300 + coords[1]);
		}


		public void drawArms()
		{
			double milliseconds_of_day = DateTime.Now.TimeOfDay.TotalMilliseconds;
			double current_hour = (milliseconds_of_day / (60.0 * 60.0 * 1000.0)) % 24.0;
			double current_minute = (milliseconds_of_day / (60.0 * 1000.0)) % 60.0;
			double current_seconds = (milliseconds_of_day / 1000.0) % 60.0;

			this.drawHoursArm(current_hour);
			this.drawMinutesArm(current_minute);
			this.drawSecondsArm(current_seconds);

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
