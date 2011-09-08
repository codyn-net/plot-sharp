using System;
using System.Collections.Generic;

namespace Plot
{
	public class Rectangle<T> where T : struct
	{
		private T d_x;
		private T d_y;
		private T d_width;
		private T d_height;
		
		public event EventHandler Resized = delegate {};
		public event EventHandler Moved = delegate {};
		
		public Rectangle(Rectangle<T> other) : this(other.X, other.Y, other.Width, other.Height)
		{
		}

		public Rectangle(T x, T y, T width, T height)
		{
			d_x = x;
			d_y = y;
			d_width = width;
			d_height = height;
		}
		
		public Rectangle() : this(default(T), default(T), default(T), default(T))
		{
		}
		
		public Rectangle<T> Copy()
		{
			return new Rectangle<T>(this);
		}
		
		public void Update(T x, T y, T width, T height)
		{
			Move(x, y);
			Resize(width, height);
		}
		
		public void Resize(T width, T height)
		{
			if (Eq(d_width, width) && Eq(d_height, height))
			{
				return;
			}
			
			d_width = width;
			d_height = height;
			
			Resized(this, new EventArgs());
		}
		
		public void Move(T x, T y)
		{
			if (Eq(d_x, x) && Eq(d_y, y))
			{
				return;
			}
			
			d_x = x;
			d_y = y;
			
			Moved(this, new EventArgs());
		}
		
		private bool Eq(T a, T b)
		{
			return EqualityComparer<T>.Default.Equals(a, b);
		}
		
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			
			Rectangle<T> other = obj as Rectangle<T>;
			
			if (other == null)
			{
				return false;
			}
			
			return Eq(d_x, other.d_x) &&
			       Eq(d_y, other.d_y) &&
			       Eq(d_width, other.d_width) &&
			       Eq(d_height, other.d_height);
		}
		
		public override int GetHashCode()
		{
			return base.GetHashCode();
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
		
		public T Width
		{
			get
			{
				return d_width;
			}
			set
			{
				if (!Eq(d_width, value))
				{
					d_width = value;
					
					Resized(this, new EventArgs());
				}
			}
		}
		
		public T Height
		{
			get
			{
				return d_height;
			}
			set
			{
				if (!Eq(d_height, value))
				{
					d_height = value;
				
					Resized(this, new EventArgs());
				}
			}
		}
	}
}

