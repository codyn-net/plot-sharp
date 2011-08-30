using System;

namespace Plot.Export
{
	public class Pdf : Exporter
	{
		private string d_filename;

		public Pdf(Graph graph, string filename) : base(graph)
		{
			d_filename = filename;
		}
		
		public override void Export()
		{
			Cairo.PdfSurface pdf = new Cairo.PdfSurface(d_filename, Dimensions.Width, Dimensions.Height);
			
			using (Cairo.Context ctx = new Cairo.Context(pdf))
			{
				Graph.DrawTo(ctx, Dimensions);
			}
			
			pdf.Destroy();
		}
	}
}

