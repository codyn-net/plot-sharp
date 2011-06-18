using System;

namespace Plot
{
	public class Color
	{
		private double d_r;
		private double d_g;
		private double d_b;
		private double d_a;
		
		public event EventHandler Changed = delegate {};

		public Color(double r, double g, double b, double a)
		{
			d_r = r;
			d_g = g;
			d_b = b;
			d_a = a;
		}
		
		public Color(double r, double g, double b) : this(r, g, b, 1)
		{
		}
		
		public Color Copy()
		{
			return new Color(d_r, d_g, d_b, d_a);
		}
		
		private void SetAndChange(ref double field, double val)
		{
			if (UpdateValue(ref field, val))
			{
				Changed(this, new EventArgs());
			}
		}
		
		public double R
		{
			get { return d_r; }
			set { SetAndChange(ref d_r, value); }
		}
		
		public double G
		{
			get { return d_g; }
			set { SetAndChange(ref d_g, value); }
		}
		
		public double B
		{
			get { return d_b; }
			set { SetAndChange(ref d_b, value); }
		}
		
		public double A
		{
			get { return d_a; }
			set { SetAndChange(ref d_a, value); }
		}
		
		private bool UpdateValue(ref double field, double val)
		{
			if (field == val)
			{
				return false;
			}
			
			field = val;
			return true;
		}
		
		public void Update(Color other)
		{
			Update(other.R, other.G, other.G, other.A);
		}
		
		public void Update(double r, double g, double b)
		{
			Update(r, g, b, 1);
		}
		
		public void Update(double r, double g, double b, double a)
		{
			bool changed = false;

			changed |= UpdateValue(ref d_r, r);
			changed |= UpdateValue(ref d_g, g);
			changed |= UpdateValue(ref d_b, b);
			changed |= UpdateValue(ref d_a, a);
			
			if (changed)
			{
				Changed(this, new EventArgs());
			}
		}
	}
}

