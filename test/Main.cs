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
			Gtk.WindowGroup grp = new Gtk.WindowGroup();
			grp.AddWindow(window);
			
			window.SetDefaultSize(400, 300);
			Plot.Widget area = new Plot.Widget();
			
			d_graph = area.Graph;

			d_graph.AxisAspect = 1;
			d_graph.KeepAspect = true;

			window.Add(area);
			
			window.ShowAll();
			window.DeleteEvent += delegate {
				Gtk.Application.Quit();
			};

			d_i = 0;

			Plot.Series.Point s1 = new Plot.Series.Point("test 1");
			s1.Size = 5;
			s1.ShowLines = true;

			Plot.Series.Line s2 = new Plot.Series.Line("test 2");
		
			d_graph.ShowRuler = true;

			d_graph.Add(s1);
			d_graph.Add(s2);
			
			List<Plot.Point<double>> d1 = new List<Plot.Point<double>>();
			List<Plot.Point<double>> d2 = new List<Plot.Point<double>>();

			int samplesize = 1000;
			
			//GLib.Timeout.Add(10, delegate {
			for (int i = 0; i <= samplesize; ++i)
			{
				d_i = i;

				double x = d_i++ / (double)samplesize;
				
				Plot.Point<double> pt1 = new Plot.Point<double>(x, Math.Sin(x * Math.PI * 2));
				Plot.Point<double> pt2 = new Plot.Point<double>(x, Math.Cos(x * Math.PI * 2));
				
				d1.Add(pt1);
				d2.Add(pt2);
			}
				
			s1.Data = d1;
			s2.Data = d2;
			
			Gtk.Application.Run();
		}
	}
}

