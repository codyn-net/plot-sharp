using System;

namespace Plot.Export
{
	public class Svg : Exporter
	{
		private string d_filename;

		public Svg(string filename, int width, int height) : base(width, height)
		{
			d_filename = filename;
		}
		
		protected override Cairo.Surface CreateSurface()
		{
			return new Cairo.SvgSurface(d_filename, Width, Height);
		}
	}
}

