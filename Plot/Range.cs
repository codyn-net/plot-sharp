using System;
using System.Collections.Generic;

namespace Plot
{
	public class Range<T> where T : struct
	{
		private T d_min;
		private T d_max;
		
		public event EventHandler Changed = delegate {};
		
		public Range(T min, T max)
		{
			d_min = min;
			d_max = max;
		}
		
		public Range() : this(default(T), default(T))
		{
		}
		
		public Range<T> Copy()
		{
			return new Range<T>(d_min, d_max);
		}
		
		private bool Eq(T a, T b)
		{
			return EqualityComparer<T>.Default.Equals(a, b);
		}
		
		public T Max
		{
			get
			{
				return d_max;
			}
			set
			{
				if (!Eq(d_max, value))
				{
					d_max = value;
					
					Changed(this, new EventArgs());
				}
			}
		}
		
		public T Min
		{
			get
			{
				return d_min;
			}
			set
			{
				if (!Eq(d_min, value))
				{
					d_min = value;
					
					Changed(this, new EventArgs());
				}
			}
		}
		
		public void Update(T min, T max)
		{
			if (Eq(d_min, min) && Eq(d_max, max))
			{
				return;
			}
			
			d_min = min;
			d_max = max;
			
			Changed(this, new EventArgs());
		}
	}
}

