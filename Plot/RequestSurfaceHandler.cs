using System;

namespace Plot
{
	public class RequestSurfaceArgs
	{
		public int Width;
		public int Height;
		public Cairo.Surface Surface;
		
		internal RequestSurfaceArgs(int width, int height)
		{
			Width = width;
			Height = height;
		}
	}

	public delegate void RequestSurfaceHandler(object source, RequestSurfaceArgs args);
}

