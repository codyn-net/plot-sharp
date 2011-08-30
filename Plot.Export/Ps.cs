using System;

namespace Plot.Export
{
	public class Ps : Exporter
	{
		private string d_filename;

		public Ps(Graph graph, string filename) : base(graph)
		{
			d_filename = filename;
		}
		
		public override void Export()
		{
			Cairo.PSSurface ps = new Cairo.PSSurface(d_filename, Dimensions.Width, Dimensions.Height);
			
			using (Cairo.Context ctx = new Cairo.Context(ps))
			{
				Graph.DrawTo(ctx, Dimensions);
			}
			
			ps.Destroy();
		}
	}
}

