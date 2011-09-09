using System;
using System.Collections.Generic;

namespace Plot
{
	public class Rectangle
	{
		private double d_x;
		private double d_y;
		private double d_width;
		private double d_height;
		
		public event EventHandler Resized = delegate {};
		public event EventHandler Moved = delegate {};
		
		public Rectangle(Rectangle other) : this(other.X, other.Y, other.Width, other.Height)
		{
		}

		public Rectangle(double x, double y, double width, double height)
		{
			d_x = x;
			d_y = y;
			d_width = width;
			d_height = height;
		}
		
		public Rectangle() : this(0, 0, 0, 0)
		{
		}
		
		public Rectangle Copy()
		{
			return new Rectangle(this);
		}
		
		public void Update(double x, double y, double width, double height)
		{
			Move(x, y);
			Resize(width, height);
		}
		
		public void Resize(double width, double height)
		{
			if (d_width == width && d_height == height)
			{
				return;
			}
			
			d_width = width;
			d_height = height;
			
			Resized(this, new EventArgs());
		}
		
		public void Move(double x, double y)
		{
			if (d_x == x && d_y == y)
			{
				return;
			}
			
			d_x = x;
			d_y = y;
			
			Moved(this, new EventArgs());
		}
		
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			
			Rectangle other = obj as Rectangle;
			
			if (other == null)
			{
				return false;
			}
			
			return d_x == other.d_x &&
			       d_y == other.d_y &&
			       d_width == other.d_width &&
			       d_height == other.d_height;
		}
		
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
		
		public double X
		{
			get
			{
				return d_x;
			}
			set
			{
				if (d_x != value)
				{
					d_x = value;
					
					Moved(this, new EventArgs());
				}
			}
		}
		
		public double Y
		{
			get
			{
				return d_y;
			}
			
			set
			{
				if (d_y != value)
				{
					d_y = value;
					
					Moved(this, new EventArgs());
				}
			}
		}
		
		public double Width
		{
			get
			{
				return d_width;
			}
			set
			{
				if (d_width != value)
				{
					d_width = value;
					
					Resized(this, new EventArgs());
				}
			}
		}
		
		public double Height
		{
			get
			{
				return d_height;
			}
			set
			{
				if (d_height != value)
				{
					d_height = value;
				
					Resized(this, new EventArgs());
				}
			}
		}
		
		public bool Contains(Biorob.Math.Point pt)
		{
			return Contains(pt.X, pt.Y);
		}
		
		public bool Contains(double x, double y)
		{
			return x >= d_x && x <= d_x + d_width &&
			       y >= d_y && y <= d_y + d_height;
		}
	}
}

