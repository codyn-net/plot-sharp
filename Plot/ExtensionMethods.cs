using System;

namespace Plot
{
	public static class ExtensionMethods
	{
		public static int Span(this Range<int> r)
		{
			return r.Max - r.Min;
		}
	
		public static double Span(this Range<double> r)
		{
			return r.Max - r.Min;
		}
		
		public static float Span(this Range<float> r)
		{
			return r.Max - r.Min;
		}
		
		public static void Shift(this Range<int> r, int size)
		{
			r.Update(r.Min + size, r.Max + size);
		}
		
		public static void Shift(this Range<double> r, double size)
		{
			r.Update(r.Min + size, r.Max + size);
		}
		
		public static void Shift(this Range<float> r, float size)
		{
			r.Update(r.Min + size, r.Max + size);
		}
		
		public static void Expand(this Range<double> r, double size)
		{
			r.Update(r.Min - size / 2, r.Max + size / 2);
		}
		
		public static bool Contains(this Rectangle<double> r, Point<double> pos)
		{
			return pos.X >= r.X && pos.X <= r.X + r.Width &&
			       pos.Y >= r.Y && pos.Y <= r.Y + r.Height;
		}
		
		public static Range<double> Widen(this Range<double> r, double scale)
		{
			if (System.Math.Abs(r.Span()) < 1e-9)
			{
				return new Range<double>(r.Min - scale, r.Max + scale);
			}
			else
			{
				double df = (r.Max - r.Min) * scale;
				
				return new Range<double>(r.Min - df, r.Max + df);
			}
		}
	}
}

