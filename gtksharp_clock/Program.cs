using System;
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
			var win = new ClockWindow();
			win.Show();
			Application.Run();
		}
	}

	/// <summary>
	/// A representation of time, with only hours, minutes and seconds.
	/// This is intended as a slightly "smart" struct. The class is a wrapper for
	/// three doubles which can be set via batch update.
	/// 
	/// Note that each attribute contains the time data of the more granular attibutes, so
	/// the time 00:30 will return both CurrentHours as 0.5 and CurrentMinutes as 30.0.
	/// </summary>
	public class TimeHMS
	{
		private double current_hours;
		private double current_minutes;
		private double current_seconds;

		public TimeHMS()
		{
			UpdateTime();
		}

		/// <summary>
		/// Updates the HMS representation to the current time.
		/// </summary>
		public void UpdateTime()
		{
			double milliseconds_of_day = DateTime.Now.TimeOfDay.TotalMilliseconds;
			UpdateTime(milliseconds_of_day);
		}

		/// <summary>
		/// Updates the HMS representation to a specified time.
		/// </summary>
		/// <param name="milliseconds_of_day">Milliseconds of day.</param>
		public void UpdateTime(double milliseconds_of_day)
		{
			current_hours = (milliseconds_of_day / (60.0 * 60.0 * 1000.0)) % 24.0;
			current_minutes = (milliseconds_of_day / (60.0 * 1000.0)) % 60.0;
			current_seconds = (milliseconds_of_day / 1000.0) % 60.0;
		}

		/// <summary>
		/// Gets the current hours.
		/// </summary>
		/// <value>The current hours.</value>
		public double CurrentHours
		{
			get { return current_hours; }
		}

		/// <summary>
		/// Gets the current minutes.
		/// </summary>
		/// <value>The current minutes.</value>
		public double CurrentMinutes
		{
			get { return current_minutes; }
		}

		/// <summary>
		/// Gets the current seconds.
		/// </summary>
		/// <value>The current seconds.</value>
		public double CurrentSeconds
		{
			get { return current_seconds; }
		}

		/// <summary>
		/// Convert back to milliseconds of day.
		/// </summary>
		/// <value>The milliseconds of day.</value>
		public double MillisecondsOfDay
		{
			get { return current_hours * 3600.0 * 1000.0; }
		}
	}

	/// <summary>
	/// Test harness for TimeHMS.
	/// </summary>
	public class TimeHMSTests
	{

		/// <summary>
		/// Verify the HMS conversions.
		/// </summary>
		/// <param name="milliseconds_of_day">Milliseconds of day.</param>
		/// <param name="expected_hours">Expected hours.</param>
		/// <param name="expected_minutes">Expected minutes.</param>
		/// <param name="expected_seconds">Expected seconds.</param>
		/// <param name="epsilon">Epsilon.</param>
		[Test]
		[TestCase(0, 0, 0, 0, 0.001)]
		[TestCase(3600.0 * 1000.0, 1.0, 0, 0, 0.000001)] // 1h.
		[TestCase(60.0 * 1000.0, 1.0/60, 1.0, 0, 0.0000001)] // 1m.
		[TestCase(1.0 * 1000.0, 1.0 / 3600.0, 1/60.0, 1.0, 0.0000001)] // 1s.
		[TestCase(50182113.33, 13.939475925925926, 56.36855555555555, 22.11333, 0.0000001)] // 13:56:22.11333
		public void ConversionHMS(double milliseconds_of_day, double expected_hours, double expected_minutes, double expected_seconds, double epsilon)
		{
			var hms = new TimeHMS();
			hms.UpdateTime(milliseconds_of_day);
			Assert.IsTrue(hms.MillisecondsOfDay - epsilon <= milliseconds_of_day && milliseconds_of_day <= hms.MillisecondsOfDay + epsilon);
			Assert.IsTrue(hms.CurrentHours - epsilon <= expected_hours && expected_hours <= hms.CurrentHours + epsilon);
			Assert.IsTrue(hms.CurrentMinutes - epsilon <= expected_minutes && expected_minutes <= hms.CurrentMinutes + epsilon);
			Assert.IsTrue(hms.CurrentSeconds - epsilon <= expected_seconds && expected_seconds <= hms.CurrentSeconds + epsilon);
		}

		/// <summary>
		/// Test that updating a TimeHMS object
		/// results in changed values from getters.
		/// 
		/// </summary>
		/// <param name="time_pre">Time pre.</param>
		/// <param name="time_post">Time post.</param>
		[Test]
		[TestCase(3400.0, 3232.0)]
		[TestCase(3500.0, 0.0)]
		public void UpdateCausesChange(double time_pre, double time_post)
		{
			var hms = new TimeHMS();

			var epsilon = 0.001;
			hms.UpdateTime(time_pre);
			Assert.IsTrue(hms.MillisecondsOfDay - epsilon <= time_pre && time_pre <= hms.MillisecondsOfDay + epsilon);

			var pre_hours = hms.CurrentHours;
			var pre_minutes = hms.CurrentMinutes;
			var pre_seconds = hms.CurrentSeconds;

			hms.UpdateTime(time_post);
			Assert.IsTrue(hms.MillisecondsOfDay - epsilon <= time_post && time_post <= hms.MillisecondsOfDay + epsilon);

			Assert.AreNotEqual(hms.CurrentHours, pre_hours);
			Assert.AreNotEqual(hms.CurrentMinutes, pre_minutes);
			Assert.AreNotEqual(hms.CurrentSeconds, pre_seconds);
		}

		/// <summary>
		/// Tests that we can round-trip convert from various times.
		/// </summary>
		[Test]
		[TestCase(0.0, 0.001)]
		[TestCase(0.001, 0.001)]
		[TestCase(13.4 * 3600.0 * 1000.0, 0.001)]
		public void ConversionRoundtrip(double milliseconds_of_day, double epsilon)
		{
			var hms = new TimeHMS();

			hms.UpdateTime(milliseconds_of_day);
			var reconverted = hms.MillisecondsOfDay;
			Assert.IsTrue(milliseconds_of_day - epsilon <= reconverted && reconverted <= milliseconds_of_day + epsilon);

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

			var cf = new ClockFace();
			var colors = ClockColors.Instance;

			this.ModifyBg(StateType.Normal, colors.grey);
			cf.ModifyBg(StateType.Normal, colors.grey);

			this.ModifyFg(StateType.Normal, colors.black);
			cf.ModifyFg(StateType.Normal, colors.black);
			this.DeleteEvent += DeleteWindow;

			Add(cf);
			ShowAll();

			new ClockTimer(this);
		}

		static void DeleteWindow(object obj, DeleteEventArgs args)
		{
			Application.Quit();
		}
	}

	class ClockFace : DrawingArea
	{

		private TimeHMS time_hms;

		public ClockFace() : base()
		{

			this.SetSizeRequest(600, 600);
			this.ExposeEvent += OnExposed;
			this.time_hms = new TimeHMS();

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
		public int[] ConvertRadialCoords(double direction, double wraparound, double length)
		{
			if (wraparound <= 0.0)
			{
				var message = String.Format("Expected a range greater than 0.0, got {0}.", wraparound);
				throw new ArithmeticException(message);
			}

			// Get the coords for the arm end.
			int[] toreturn = new int[2];
			toreturn[0] = (int)Math.Round(length * Math.Sin(2.0 * Math.PI * direction / wraparound));
			toreturn[1] = (int)Math.Round(-length * Math.Cos(2.0 * Math.PI * direction / wraparound));
			return toreturn;
		}

		public void OnExposed(object o, ExposeEventArgs args)
		{
			this.time_hms.UpdateTime();
			this.DrawFaceMarkings();
			this.DrawArms();
		}


		private void DrawArmHours(double current_hours)
		{
			var colors = ClockColors.Instance;
			var gc = this.Style.BaseGC(StateType.Normal);
			gc.SetLineAttributes(2, Gdk.LineStyle.Solid, Gdk.CapStyle.Butt, Gdk.JoinStyle.Miter);

			int[][] coords = {
				this.ConvertRadialCoords(current_hours, 12.0, -20),
				this.ConvertRadialCoords(current_hours - 4.0, 12.0, 20),
				this.ConvertRadialCoords(current_hours - 0.1, 12.0, 100),
				this.ConvertRadialCoords(current_hours + 0.1, 12.0, 100),
				this.ConvertRadialCoords(current_hours + 4.0, 12.0, 20),
				this.ConvertRadialCoords(current_hours, 12.0, -20)
			};

			var thepoints = from coord in coords
							select new Gdk.Point(coord[0] + 300, coord[1] + 300);

			gc.RgbFgColor = colors.dark_grey;
			this.GdkWindow.DrawPolygon(gc, true, thepoints.ToArray());

			gc.RgbFgColor = colors.white_smoke;
			this.GdkWindow.DrawPolygon(gc, false, thepoints.ToArray());
		}

		private void DrawArmMinutes(double current_minutes)
		{

			var colors = ClockColors.Instance;
			var gc = this.Style.BaseGC(StateType.Normal);
			gc.SetLineAttributes(2, Gdk.LineStyle.Solid, Gdk.CapStyle.Butt, Gdk.JoinStyle.Miter);

			int[][] coords = {
				this.ConvertRadialCoords(current_minutes, 60.0, -15),
				this.ConvertRadialCoords(current_minutes - 20, 60.0, 15),
				this.ConvertRadialCoords(current_minutes - 0.35, 60.0, 205),
				this.ConvertRadialCoords(current_minutes, 60.0, 215),
				this.ConvertRadialCoords(current_minutes + 0.35, 60.0, 205),
				this.ConvertRadialCoords(current_minutes + 20, 60.0, 15),
				this.ConvertRadialCoords(current_minutes, 60.0, -15)
			};

			var thepoints = from coord in coords
							select new Gdk.Point(coord[0] + 300, coord[1] + 300);

			gc.RgbFgColor = colors.dark_grey;

			this.GdkWindow.DrawPolygon(gc, true, thepoints.ToArray());

			gc.RgbFgColor = colors.white_smoke;
			this.GdkWindow.DrawPolygon(gc, false, thepoints.ToArray());

		}

		private void DrawArmSeconds(double current_seconds)
		{
			var colors = ClockColors.Instance;
			var gc = this.Style.BaseGC(StateType.Normal);
			gc.RgbFgColor = colors.blue;
			gc.SetLineAttributes(5, Gdk.LineStyle.Solid, Gdk.CapStyle.Round, Gdk.JoinStyle.Round);

			var coords = this.ConvertRadialCoords(current_seconds, 60.0, 230);
			this.GdkWindow.DrawLine(gc, 300, 300, 300 + coords[0], 300 + coords[1]);
			this.GdkWindow.DrawArc(gc, true, 300 - 7, 300 - 7, 7 * 2, 7 * 2, 0, 360 * 64);
		}


		public void DrawArms()
		{
			var current_hour = time_hms.CurrentHours;
			var current_minute = time_hms.CurrentMinutes;
			var current_seconds = time_hms.CurrentSeconds;

			this.DrawArmHours(current_hour);
			this.DrawArmMinutes(current_minute);
			this.DrawArmSeconds(current_seconds);
		}

		/// <summary>
		/// Call to draw the ticks around the edge of the clock face.
		/// Only drawstyle supported is 12 black tick-marks.
		/// </summary>
		private void DrawFaceMarkings()
		{

			var colors = ClockColors.Instance;
			var gc = this.Style.BaseGC(StateType.Normal);
			gc.SetLineAttributes(1, Gdk.LineStyle.Solid, Gdk.CapStyle.Butt, Gdk.JoinStyle.Miter);
			gc.RgbFgColor = colors.black;

			for (var hour = 0; hour <= 12; hour++)
			{
				int[][] coords = {
							this.ConvertRadialCoords(hour - 0.02, 12.0, 240),
							this.ConvertRadialCoords(hour - 0.02, 12.0, 270),
							this.ConvertRadialCoords(hour + 0.02, 12.0, 270),
							this.ConvertRadialCoords(hour + 0.02, 12.0, 240)
						};

				var thepoints = from coord in coords
								select new Gdk.Point(coord[0] + 300, coord[1] + 300);

				this.GdkWindow.DrawPolygon(gc, true, thepoints.ToArray());
			}
		}
	}

	[TestFixture]
	public class ClockFaceTest
	{
		[Test]
		public void TestArmLength()
		{
			var cf = new ClockFace();
			var coords = new int[0];

			var values = new double[] { -200, 0, 11.11, 20000 };
			var maxes = new double[] { 20, 200 };
			var lengths = new double[] { 4, 222, -50 };

			foreach (double length in lengths)
			{
				foreach (double value in values)
				{
					foreach (double max in maxes)
					{
						coords = cf.ConvertRadialCoords(value, max, length);
						var hyp_actual = coords[0] * coords[0] + coords[1] * coords[1];
						var abs_len = Math.Abs(length);
						var lower_expected = (abs_len - 1) * (abs_len - 1);
						var upper_expected = (abs_len + 1) * (abs_len + 1);
						var message = String.Format("Failed reasonable hypothenuse interval {0} <= {1}, <= {2}", lower_expected * 0.99, hyp_actual, upper_expected);
						Assert.IsTrue(lower_expected <= hyp_actual && hyp_actual <= upper_expected, message);
					}
				}
			}
		}

		[Test]
		public void TestArmEnd()
		{
			var cf = new ClockFace();


			Assert.AreEqual(2, cf.ConvertRadialCoords(0.0, 60.0, 300.0).Length);

			// Hour hand for three o'clock.
			var coords = cf.ConvertRadialCoords(3.0, 12.0, 300.0);
			Assert.IsTrue(299.0 <= coords[0] && coords[0] <= 300.0, "Actual coords are {0}, {1}", coords[0], coords[1]);


			// Hour hand for 7:30.  -> -71, 71.
			coords = cf.ConvertRadialCoords(7.5, 12.0, 100.0);
			Assert.IsTrue(-73.0 <= coords[0] && coords[0] <= -70.0, "For 7:30 x, actual coords are {0}, {1}", coords[0], coords[1]);
			Assert.IsTrue(70.0 <= coords[1] && coords[1] <= 73.0, "For 7:30 y, actual coords are {0}, {1}", coords[0], coords[1]);
		}
	}
}
