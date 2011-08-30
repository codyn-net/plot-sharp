using System;

namespace Plot.Export
{
	public class Png : Exporter
	{
		private string d_filename;

		public Png(Graph graph, string filename) : base(graph)
		{
			d_filename = filename;
		}
		
		public override void Export()
		{
			Cairo.ImageSurface png = new Cairo.ImageSurface(Cairo.Format.ARGB32, Dimensions.Width, Dimensions.Height);

			using (Cairo.Context ctx = new Cairo.Context(png))
			{
				Graph.DrawTo(ctx, Dimensions);
			}
			
			png.WriteToPng(d_filename);			
			png.Destroy();
		}
	}
}

