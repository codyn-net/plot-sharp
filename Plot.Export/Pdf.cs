using System;

namespace Plot.Export
{
	public class Pdf : Exporter
	{
		private string d_filename;

		public Pdf(string filename, int width, int height) : base(width, height)
		{
			d_filename = filename;
		}
		
		protected override Cairo.Surface CreateSurface()
		{
			return new Cairo.PdfSurface(d_filename, Width, Height);
		}		
	}
}

