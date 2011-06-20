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
		
		public Color(string html)
		{
			if (!html.StartsWith("#"))
			{
				return;
			}
			
			string rest = html.Substring(1);
			
			if (rest.Length == 3)
			{
				rest = new string(new char[] {rest[0], rest[0], rest[1], rest[1], rest[2], rest[2]});
			}
			else if (rest.Length == 4)
			{
				rest = new string(new char[] {rest[0], rest[0], rest[1], rest[1], rest[2], rest[2], rest[3], rest[3]});
			}
			
			if (rest.Length == 6)
			{
				rest += "ff";	
			}
			
			if (rest.Length != 8)
			{
				return;
			}
			
			d_r = FromHex(rest.Substring(0, 2));
			d_g = FromHex(rest.Substring(2, 2));
			d_b = FromHex(rest.Substring(4, 2));
			d_a = FromHex(rest.Substring(6, 2));
		}
		
		private double FromHex(string hex)
		{
			return Convert.ToInt32(hex, 16) / 255.0;
		}

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

