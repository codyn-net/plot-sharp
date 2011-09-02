using System;

namespace Plot.Export
{
	public class Png : Exporter
	{
		private string d_filename;

		public Png(string filename, int width, int height) : base(width, height)
		{
			d_filename = filename;
		}
		
		protected override Cairo.Surface CreateSurface()
		{
			return new Cairo.ImageSurface(Cairo.Format.ARGB32, Width, Height);
		}
		
		public override void End()
		{
			Surface.WriteToPng(d_filename);
			base.End();
		}
	}
}

