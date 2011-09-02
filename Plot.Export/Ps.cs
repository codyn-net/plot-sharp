using System;

namespace Plot.Export
{
	public class Ps : Exporter
	{
		private string d_filename;

		public Ps(string filename, int width, int height) : base(width, height)
		{
			d_filename = filename;
		}
		
		protected override Cairo.Surface CreateSurface()
		{
			return new Cairo.PSSurface(d_filename, Width, Height);
		}		
	}
}

