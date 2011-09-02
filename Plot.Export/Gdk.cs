using System;
using GGdk = Gdk;

namespace Plot.Export
{
	public class Gdk : Exporter
	{
		private GGdk.Drawable d_drawable;

		public Gdk(GGdk.Drawable drawable)
		{
			d_drawable = drawable;
			
			int width;
			int height;
			
			d_drawable.GetSize(out width, out height);
			
			Width = width;
			Height = height;
		}
		
		protected override Cairo.Context CreateContext()
		{
			return GGdk.CairoHelper.Create(d_drawable);
		}
	}
}

