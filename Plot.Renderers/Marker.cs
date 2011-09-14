using System;
using Biorob.Math;

namespace Plot.Renderers
{
	public abstract class Marker
	{
		private static Point s_triangleDxDy;

		public delegate void Renderer(Cairo.Context context, Point scale, Point item, double msize, double lw);
		
		static Marker()
		{
			s_triangleDxDy = new Point(Math.Sin(1 / 3.0 * Math.PI),
			                           Math.Cos(1 / 3.0 * Math.PI));
		}

		public static Renderer Lookup(MarkerStyle style)
		{
			switch (style)
			{
				case MarkerStyle.Circle:
					return RenderCircle;
				case MarkerStyle.FilledCircle:
					return RenderFilledCircle;
				case MarkerStyle.FilledSquare:
					return RenderFilledSquare;
				case MarkerStyle.Square:
					return RenderSquare;
				case MarkerStyle.Cross:
					return RenderCross;
				case MarkerStyle.Triangle:
					return RenderTriangle;
				case MarkerStyle.FilledTriangle:
					return RenderFilledTriangle;
				case MarkerStyle.Dash:
					return RenderDash;
				case MarkerStyle.None:
					return null;
				default:
					throw new NotImplementedException();
			}
		}
		
		private static void MakeCircle(Cairo.Context context, Point scale, Point item, double msize, double lw)
		{
			context.Arc(item.X * scale.X, item.Y * scale.Y, (msize - lw) / 2, 0, 2 * Math.PI);
		}
		
		private static void MakeSquare(Cairo.Context context, Point scale, Point item, double msize, double lw)
		{
			double size = msize - lw;

			context.MoveTo(item.X * scale.X, item.Y * scale.Y);
			context.RelMoveTo(-size / 2, -size /  2);
			context.RelLineTo(size, 0);
			context.RelLineTo(0, size);
			context.RelLineTo(-size, 0);
			context.ClosePath();
		}

		private static void MakeTriangle(Cairo.Context context, Point scale, Point item, double msize, double lw)
		{
			double halfsize = (msize - lw) / 2;

			context.MoveTo(item.X * scale.X, item.Y * scale.Y);
			context.RelMoveTo(0, halfsize);
			
			double dx = halfsize * s_triangleDxDy.X;
			double dy = halfsize * s_triangleDxDy.Y;
			
			context.RelLineTo(dx, -halfsize - dy);
			context.RelLineTo(2 * -dx, 0);
			context.ClosePath();
		}

		public static void RenderCircle(Cairo.Context context, Point scale, Point item, double msize, double lw)
		{
			MakeCircle(context, scale, item, msize, lw);
			context.Stroke();
		}
		
		public static void RenderFilledCircle(Cairo.Context context, Point scale, Point item, double msize, double lw)
		{
			MakeCircle(context, scale, item, msize, lw);
			context.FillPreserve();
			context.Stroke();
		}
		
		public static void RenderSquare(Cairo.Context context, Point scale, Point item, double msize, double lw)
		{
			MakeSquare(context, scale, item, msize, lw);
			context.Stroke();
		}
		
		public static void RenderFilledSquare(Cairo.Context context, Point scale, Point item, double msize, double lw)
		{
			MakeSquare(context, scale, item, msize, lw);
			context.FillPreserve();
			context.Stroke();
		}
		
		public static void RenderTriangle(Cairo.Context context, Point scale, Point item, double msize, double lw)
		{
			MakeTriangle(context, scale, item, msize, lw);
			context.Stroke();
		}
		
		public static void RenderFilledTriangle(Cairo.Context context, Point scale, Point item, double msize, double lw)
		{
			MakeTriangle(context, scale, item, msize, lw);
			
			context.FillPreserve();
			context.Stroke();
		}

		public static void RenderCross(Cairo.Context context, Point scale, Point item, double msize, double lw)
		{
			context.MoveTo(item.X * scale.X, item.Y * scale.Y);
			context.RelMoveTo(-msize / 2, -msize / 2);
			context.RelLineTo(msize, msize);
			context.Stroke();
			
			context.MoveTo(item.X * scale.X, item.Y * scale.Y);
			context.RelMoveTo(msize / 2, -msize / 2);
			context.RelLineTo(-msize, msize);
			context.Stroke();
		}
		
		public static void RenderDash(Cairo.Context context, Point scale, Point item, double msize, double lw)
		{
			context.MoveTo(item.X * scale.X - msize / 2, item.Y * scale.Y);
			context.RelLineTo(msize, 0);
			
			context.Stroke();
		}
	}
}

