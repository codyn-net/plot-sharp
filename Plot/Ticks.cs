using System;
using System.Collections.Generic;
using System.Collections;

namespace Plot
{
	public class Ticks : Changeable, IEnumerable<double>
	{
		private List<double> d_ticks;

		private double d_maxDecimals;
		private double d_minTickSize;
		private double d_tickSize;
		
		private double d_calculatedTickSize;
		private double d_calculatedTickDecimals;
		
		private bool d_visible;
		private double d_length;
		private bool d_showLabels;
		
		public Ticks()
		{
			d_ticks = new List<double>();

			d_maxDecimals = -1;
			d_minTickSize = -1;
			d_tickSize = -1;
			d_visible = true;
			d_length = 5;
			d_showLabels = true;
		}
		
		public IEnumerator<double> GetEnumerator()
		{
			return d_ticks.GetEnumerator();
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return d_ticks.GetEnumerator();
		}
		
		public bool ShowLabels
		{
			get
			{
				return d_showLabels;
			}
			set
			{
				if (d_showLabels != value)
				{
					d_showLabels = value;
					EmitChanged();
				}
			}
		}
		
		public double Length
		{
			get
			{
				return d_length;
			}
			set
			{
				if (d_length != value)
				{
					d_length = value;
					EmitChanged();
				}
			}
		}
		
		public bool Visible
		{
			get
			{
				return d_visible;
			}
			set
			{
				if (value != d_visible)
				{
					d_visible = value;
					EmitChanged();
				}
			}
		}
		
		public double MaxDecimals
		{
			get
			{
				return d_maxDecimals;
			}
			set
			{
				if (d_maxDecimals != value)
				{
					d_maxDecimals = value;
					EmitChanged();
				}
			}
		}
		
		public double MinTickSize
		{
			get
			{
				return d_minTickSize;
			}
			set
			{
				if (d_minTickSize != value)
				{
					d_minTickSize = value;
					EmitChanged();
				}
			}
		}
		
		public double TickSize
		{
			get
			{
				return d_tickSize;
			}
			set
			{
				if (d_tickSize != value)
				{
					d_tickSize = value;
					EmitChanged();
				}
			}
		}
		
		public double CalculatedTickSize
		{
			get
			{
				return d_calculatedTickSize;
			}
		}
		
		public double CalculatedTickDecimals
		{
			get
			{
				return d_calculatedTickDecimals;
			}
		}
		
		private double FloorInBase(double n, double b)
		{
			return b * Math.Floor(n / b);
		}
		
		internal void Update(Range<double> range, int pixelSpan)
		{
			d_ticks.Clear();

			// Copied from flot library			
			double num = 0.3 * Math.Sqrt(pixelSpan);
			double span = range.Span();
			double delta = span / num;
			
			double dec = -Math.Floor(Math.Log10(delta));
			
			if (d_maxDecimals > 0 && dec > d_maxDecimals)
			{
				dec = d_maxDecimals;
			}
			
			double magn = Math.Pow(10, -dec);
			double norm = delta / magn;
			double size;
			
			if (norm < 1.5)
			{
				size = 1;
			}
			else if (norm < 3)
			{
				size = 2;
				
				if (norm > 2.25 && (d_maxDecimals <= 0 || dec + 1 <= d_maxDecimals))
				{
					size = 2.5;
					++dec;
				}
			}
			else if (norm < 7.5)
			{
				size = 5;
			}
			else
			{
				size = 10;
			}
			
			size *= magn;
			
			if (d_minTickSize >= 0 && size < d_minTickSize)
			{
				size = d_minTickSize;
			}
			
			if (d_tickSize >= 0)
			{
				size = d_tickSize;
			}
			
			d_calculatedTickSize = size;
			d_calculatedTickDecimals = Math.Max(0, d_maxDecimals > 0 ? d_maxDecimals : dec);
			
			double start = FloorInBase(range.Min, size);
			double prev;
			double v = Double.NaN;
			int i = 0;
			
			do
			{
				prev = v;
				v = start + i * d_calculatedTickSize;

				d_ticks.Add(v);
				
				++i;
			} while (v < range.Max && v != prev);
		}
	}
}

