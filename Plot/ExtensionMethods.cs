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
	}
}

