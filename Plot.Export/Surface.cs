using System;

namespace Plot.Export
{
	public class Surface : Exporter
	{
		private Cairo.Surface d_surface;

		public Surface(Graph graph, Cairo.Surface surface, int width, int height) : base(graph)
		{
			d_surface = surface;
			
			Dimensions.Update(0, 0, width, height);
		}
		
		public override void Export()
		{
			using (Cairo.Context ctx = new Cairo.Context(d_surface))
			{
				Graph.DrawTo(ctx, Dimensions);
			}
		}
	}
}

