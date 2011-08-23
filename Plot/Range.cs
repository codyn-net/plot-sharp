using System;
using System.Collections.Generic;

namespace Plot
{
	public class Range<T> : Changeable where T : struct
	{
		private T d_min;
		private T d_max;
		
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
					
					EmitChanged();
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
					
					EmitChanged();
				}
			}
		}
		
		public void Update(Range<T> other)
		{
			Update(other.Min, other.Max);
		}
		
		public void Update(T min, T max)
		{
			if (Eq(d_min, min) && Eq(d_max, max))
			{
				return;
			}
			
			d_min = min;
			d_max = max;
			
			EmitChanged();
		}
		
		public override string ToString()
		{
			return String.Format("[{0}, {1}]", d_min, d_max);
		}
	}
}

