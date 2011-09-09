using System;

namespace Plot.Export
{
	public abstract class Exporter : IDisposable
	{
		private Cairo.Surface d_surface;
		private int d_width;
		private int d_height;
		
		public Exporter(int width, int height)
		{
			d_width = width;
			d_height = height;
		}
		
		public Exporter() : this(0, 0)
		{
		}
		
		public int Width
		{
			get
			{
				return d_width;
			}
			protected set
			{
				d_width = value;
			}
		}
		
		public int Height
		{
			get
			{
				return d_height;
			}
			protected set
			{
				d_height = value;
			}
		}
		
		public void Dispose()
		{
			if (d_surface != null)
			{
				d_surface.Destroy();
			}
		}
		
		protected Cairo.Surface Surface
		{
			get
			{
				return d_surface;
			}
		}

		protected virtual Cairo.Surface CreateSurface()
		{
			return null;
		}

		protected virtual Cairo.Context CreateContext()
		{
			if (d_surface == null)
			{
				return null;
			}
			
			return new Cairo.Context(d_surface);
		}
		
		public delegate void DoHandler();
		public delegate void OverlayHandler(Cairo.Context context, Graph graph, Rectangle dimensions);
		
		public virtual void Begin()
		{
			d_surface = CreateSurface();
		}
		
		public virtual void End()
		{
			if (d_surface != null)
			{
				d_surface.Destroy();
				d_surface = null;
			}
		}
		
		public virtual void Do(DoHandler handler)
		{
			Begin();
			handler();
			End();
		}
		
		public void Export(Graph graph, Rectangle dimensions)
		{
			this.Export(graph, dimensions, null);
		}
		
		public virtual void Export(Graph graph, Rectangle dimensions, OverlayHandler overlay)
		{
			using (Cairo.Context ctx = CreateContext())
			{
				if (ctx == null)
				{
					return;
				}

				graph.DrawTo(ctx, dimensions);
				
				if (overlay != null)
				{
					overlay(ctx, graph, dimensions);
				}
			}
		}
	}
}

