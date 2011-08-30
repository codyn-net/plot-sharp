using System;

namespace Plot.Export
{
	public class Svg : Exporter
	{
		private string d_filename;

		public Svg(Graph graph, string filename) : base(graph)
		{
			d_filename = filename;
		}
		
		public override void Export()
		{
			Cairo.SvgSurface svg = new Cairo.SvgSurface(d_filename, Dimensions.Width, Dimensions.Height);
			
			using (Cairo.Context ctx = new Cairo.Context(svg))
			{
				Graph.DrawTo(ctx, Dimensions);
			}
			
			svg.Destroy();
		}
	}
}

