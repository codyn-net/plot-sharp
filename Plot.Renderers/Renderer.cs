using System;

namespace Plot.Renderers
{
	public abstract class Renderer : Changeable
	{
		private Range<double> d_xrange;
		private Range<double> d_yrange;
		
		private Units d_xunits;
		private Units d_yunits;
		
		private bool d_hasRuler;
		
		public event EventHandler RulerChanged = delegate {};
		
		public Range<double> XRange
		{
			get
			{
				return d_xrange;
			}
		}
		
		public Range<double> YRange
		{
			get
			{
				return d_yrange;
			}
		}
		
		public Renderer()
		{
			d_xrange = new Range<double>();
			d_yrange = new Range<double>();
			
			d_hasRuler = false;
		}
		
		public Units XUnits
		{
			get
			{
				return d_xunits;
			}
			set
			{
				if (d_xunits != value)
				{
					d_xunits = value;
					EmitChanged();
				}
			}
		}
		
		public Units YUnits
		{
			get
			{
				return d_yunits;
			}
			set
			{
				if (d_yunits != value)
				{
					d_yunits = value;
					EmitChanged();
				}
			}
		}
		
		public virtual bool CanRule
		{
			get
			{
				return false;
			}
		}
		
		public bool HasRuler
		{
			get
			{
				return CanRule ? d_hasRuler : false;
			}
			set
			{
				if (CanRule && d_hasRuler != value)
				{
					d_hasRuler = value;
					RulerChanged(this, new EventArgs());
				}
			}
		}
		
		public Point<double> ValueAtX(double x)
		{
			bool interpolated;
			bool extrapolated;
			
			return ValueAtX(x, out interpolated, out extrapolated);
		}
		
		public virtual Point<double> ValueAtX(double x, out bool interpolated, out bool extrapolated)
		{
			interpolated = false;
			extrapolated = true;

			return new Point<double>(0, 0);
		}
		
		public virtual Point<double> ValueClosestToX(double x)
		{
			return new Point<double>(0, 0);
		}

		public abstract void Render(Cairo.Context context, Point<double> scale);
		
		public double UnitToPixel(Units units, double val, double scale)
		{
			switch (units)
			{
				case Units.Axis:
					return val * scale;
			}
			
			return val;
		}
		
		public Range<double> XRangePixels(double scale)
		{
			return new Range<double>(UnitToPixel(d_xunits, d_xrange.Min, scale),
			                         UnitToPixel(d_xunits, d_xrange.Max, scale));
		}
		
		public Range<double> YRangePixels(double scale)
		{
			return new Range<double>(UnitToPixel(d_yunits, d_yrange.Min, scale),
			                         UnitToPixel(d_yunits, d_yrange.Max, scale));
		}
		
		protected virtual Renderer CopyTo(Renderer other)
		{
			d_xrange.Update(other.d_xrange);
			d_yrange.Update(other.d_yrange);
			
			d_xunits = other.d_xunits;
			d_yunits = other.d_yunits;
			d_hasRuler = other.d_hasRuler;

			return other;
		}

		public virtual Renderer Copy()
		{
			return CopyTo((Renderer)GetType().GetConstructor(new Type[] {}).Invoke(new object[] {}));
		}
	}
}