using System;
using GGdk = Gdk;

namespace Plot.Export
{
	public class Gdk : Exporter
	{
		private GGdk.Drawable d_drawable;

		public Gdk(Graph graph, GGdk.Drawable drawable) : base(graph)
		{
			d_drawable = drawable;
			
			int w;
			int h;
			
			drawable.GetSize(out w, out h);
			Dimensions.Update(0, 0, w, h);
		}
		
		public override void Export()
		{
			using (Cairo.Context ctx = GGdk.CairoHelper.Create(d_drawable))
			{
				Graph.DrawTo(ctx, Dimensions);
			}
		}
	}
}

