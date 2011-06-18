using System;
using System.Collections.Generic;

namespace Plot
{
	public class Point<T> : IComparable<Point<T>> where T : struct, IComparable
	{
		private T d_x;
		private T d_y;
		
		public event EventHandler Moved = delegate {};

		public Point(T x, T y)
		{
			d_x = x;
			d_y = y;
		}
		
		public Point<T> Copy()
		{
			return new Point<T>(d_x, d_y);
		}
		
		public Point() : this(default(T), default(T))
		{
		}
		
		private bool Eq(T a, T b)
		{
			return EqualityComparer<T>.Default.Equals(a, b);
		}
		
		public int CompareTo(Point<T> other)
		{
			return d_x.CompareTo(other.d_x);
		}
		
		public T X
		{
			get
			{
				return d_x;
			}
			set
			{
				if (!Eq(d_x, value))
				{
					d_x = value;
					
					Moved(this, new EventArgs());
				}
			}
		}
		
		public T Y
		{
			get
			{
				return d_y;
			}
			set
			{
				if (!Eq(d_y, value))
				{
					d_y = value;
					
					Moved(this, new EventArgs());
				}
			}
		}
		
		void Move(T x, T y)
		{
			if (Eq(d_x, x) && Eq(d_y, y))
			{
				return;
			}

			d_x = x;
			d_y = y;
			
			Moved(this, new EventArgs());
		}
	}
}

