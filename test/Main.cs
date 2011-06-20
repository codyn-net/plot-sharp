using System;
using System.Collections.Generic;

namespace test
{
	public class MainClass
	{
		private static Plot.Graph d_graph;
		private static int d_i;

		public static void Main(string[] args)
		{
			Gtk.Application.Init("test", ref args);
			Gtk.Window window = new Gtk.Window("Test");
			
			window.SetDefaultSize(400, 300);
			Gtk.DrawingArea area = new Gtk.DrawingArea();
			
			d_graph = new Plot.Graph();

			d_graph.RequestSurface += delegate(object source, Plot.RequestSurfaceArgs a) {
				using (Cairo.Context ctx = Gdk.CairoHelper.Create(area.GdkWindow))
				{
					a.Surface = ctx.Target.CreateSimilar(ctx.Target.Content, a.Width, a.Height);
				}
			};
			
			d_graph.RequestRedraw += delegate {
				area.QueueDraw();
			};

			area.SizeAllocated += delegate(object o, Gtk.SizeAllocatedArgs a) {
				Console.WriteLine(a.Allocation.Width);		
				d_graph.Dimensions.Resize(a.Allocation.Width, a.Allocation.Height);
			};

			window.Add(area);
			
			window.ShowAll();
			window.DeleteEvent += delegate {
				Gtk.Application.Quit();
			};
			
			area.ExposeEvent += HandleAreaExposeEvent;
			
			d_i = 0;

			Plot.LineSeries series = new Plot.LineSeries("test");
			d_graph.Add(series);
			
			GLib.Timeout.Add(10, delegate {
				double x = (++d_i / (double)100) % 1;
				
				series.Append(new Plot.Point<double>(Math.Cos(x * Math.PI * 2), Math.Sin(x * Math.PI * 2)));
				d_graph.ProcessAppend();

				return true;
			});

			Gtk.Application.Run();
		}

		static void HandleAreaExposeEvent(object o, Gtk.ExposeEventArgs args)
		{
			using (Cairo.Context ctx = Gdk.CairoHelper.Create(args.Event.Window))
			{
				d_graph.Draw(ctx);
			}
		}
	}
}

