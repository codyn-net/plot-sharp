using System;

namespace Plot.Export
{
	public class Surface : Exporter
	{
		private Cairo.Surface d_surface;

		public Surface(Cairo.Surface surface)
		{
			d_surface = surface;
		}
		
		protected override Cairo.Context CreateContext()
		{
			return new Cairo.Context(d_surface);
		}
	}
}

