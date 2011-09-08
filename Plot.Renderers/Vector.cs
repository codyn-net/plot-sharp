using System;
using System.Collections.Generic;

namespace Plot.Renderers
{
	public class Vector : Line
	{
		public enum LengthType
		{
			Axis,
			Pixel
		}

		private List<double> d_dydx;
		private List<double> d_length;
		private List<double> d_lengthNorm;
		private double d_equalLength;
		private double d_pixelLength;
		private LengthType d_lengthType;
		private Plot.Point<double> d_scale;
		private bool d_drawArrowHead;
		private double d_arrowHeadSize;

		public Vector(IEnumerable<Point<double>> data, Color color, string label) : base(data, color, label)
		{
			d_dydx = new List<double>();
			d_length = new List<double>();
			d_lengthNorm = new List<double>();
			d_equalLength = 10;
			d_pixelLength = 0;
			d_lengthType = LengthType.Axis;
			d_drawArrowHead = true;
			d_arrowHeadSize = -1;
			
			LineStyle = Line.LineType.None;
		}
		
		public bool ShowArrowHead
		{
			get
			{
				return d_drawArrowHead;
			}
			set
			{
				if (d_drawArrowHead != value)
				{
					d_drawArrowHead = value;
					EmitChanged();
				}
			}
		}
		
		protected override Line CopyTo(Line other)
		{
			 base.CopyTo(other);
			 
			 Vector v = (Vector)other;
			 
			 v.d_equalLength = d_equalLength;
			 v.d_pixelLength = d_pixelLength;
			 v.d_scale = new Point<double>(d_scale.X, d_scale.Y);
			 v.d_drawArrowHead = d_drawArrowHead;
			 v.d_arrowHeadSize = d_arrowHeadSize;
			 v.d_lengthType = d_lengthType;
			 
			 v.d_dydx = new List<double>(d_dydx);
			 v.d_length = new List<double>(d_length);
			 v.d_lengthNorm = new List<double>(d_lengthNorm);
			 
			 return other;
		}
		
		public override Renderer Copy()
		{
			return CopyTo(new Vector());
		}
		
		public override bool CanRule
		{
			get
			{
				return false;
			}
		}
		
		public double ArrowHeadSize
		{
			get
			{
				return d_arrowHeadSize;
			}
			set
			{
				if (d_arrowHeadSize != value)
				{
					d_arrowHeadSize = value;
					EmitChanged();
				}
			}
		}
		
		public IEnumerable<double> DyDx
		{
			get
			{
				return d_dydx;
			}
			set
			{
				d_dydx.Clear();
				d_dydx.AddRange(value);
				
				Recalculate();
				
				EmitChanged();
			}
		}
		
		public double PixelLength
		{
			get
			{
				return d_pixelLength;
			}
			set
			{
				d_pixelLength = value;
				d_length.Clear();

				d_lengthType = LengthType.Pixel;
				d_scale = null;
				
				EmitChanged();
			}
		}
		
		public LengthType LengthScale
		{
			get
			{
				return d_lengthType;
			}
			set
			{
				if (value == d_lengthType)
				{
					return;
				}

				d_lengthType = value;
				
				Recalculate();
				EmitChanged();
			}
		}

		public double EqualLength
		{
			get
			{
				return d_equalLength;
			}
			set
			{
				d_equalLength = value;
				d_length.Clear();
				d_lengthType = LengthType.Axis;
				
				Recalculate();
				EmitChanged();
			}
		}
		
		private void Recalculate()
		{
			if (d_lengthType == LengthType.Pixel)
			{
				/* Do that later when we know the scale */
				d_scale = null;
				return;
			}

			d_lengthNorm.Clear();

			for (int i = 0; i < d_dydx.Count; ++i)
			{
				double l = d_equalLength;
				
				if (i < d_length.Count)
				{
					l = d_length[i];
				}

				d_lengthNorm.Add(l / Math.Sqrt(1 + d_dydx[i] * d_dydx[i]));
			}
		}
		
		public IEnumerable<double> Length
		{
			get
			{
				return d_length;
			}
			set
			{
				d_length.Clear();
				d_length.AddRange(value);
				
				Recalculate();
				
				EmitChanged();
			}
		}
		
		public Vector(IEnumerable<Point<double>> data, Color color) : this(data, color, "")
		{
		}
		
		public Vector(IEnumerable<Point<double>> data) : this(data, null, "")
		{
		}

		public Vector(Color color, string name) : this(new Point<double>[] {}, color, "")
		{
		}
		
		public Vector(string name) : this(null, "")
		{
		}
		
		public Vector() : this("")
		{
		}
		
		private bool SameScale(Point<double> scale)
		{
			if (d_scale == null)
			{
				return false;
			}
			
			return d_scale.X == scale.X && d_scale.Y == scale.Y;
		}
		
		private void RecalculatePixelLength()
		{
			/* Recalculate d_lengthNorm so that when scaled with 'scale' the length
			   is equal to d_pixelLength... */
			d_lengthNorm.Clear();

			for (int i = 0; i < d_dydx.Count; ++i)
			{
				double l = d_pixelLength;
				
				if (i < d_length.Count)
				{
					l = d_length[i];
				}

				d_lengthNorm.Add(l / Math.Sqrt(d_scale.X * d_scale.X + d_dydx[i] * d_dydx[i] * d_scale.Y * d_scale.Y));
			}
		}
		
		public override void Render(Cairo.Context context, Point<double> scale)
		{
			base.Render(context, scale);
			
			int idx = 0;
			int length = Count;
			
			int endidx;
			
			endidx = Math.Min(idx + length, d_dydx.Count);
			
			context.SetSourceRGB(Color.R, Color.G, Color.B);
			context.LineWidth = LineWidth;
			
			if (d_lengthType == LengthType.Pixel && !SameScale(scale))
			{
				d_scale = scale.Copy();
				RecalculatePixelLength();
			}
			
			for (int i = idx; i < endidx; ++i)
			{
				Point<double> item = PrivateData[i];
				
				// In pixel space
				double px = scale.X * item.X;
				double py = scale.Y * item.Y;
	
				// dx, dy from px, py to where to draw the line
				double pdx = d_lengthNorm[i] * scale.X;
				double pdy = d_lengthNorm[i] * d_dydx[i] * scale.Y;
				
				double l = Math.Sqrt(pdx * pdx + pdy * pdy);
				
				if (MarkerStyle == MarkerType.Circle)
				{
					// Start at the edge of the circle
					px += 0.5 * MarkerSize / l * pdx;
					py += 0.5 * MarkerSize / l * pdy;
				}

				context.MoveTo(px, py);
				
				// Scaling of pdx/pdy to incorporate arrow head
				double s = 1;
				double ars = MarkerSize;
				
				if (d_arrowHeadSize > 0)
				{
					ars = d_arrowHeadSize;
				}
				
				if (d_drawArrowHead)
				{
					s = (l - ars) / l;
				}
				
				double pendx = px + pdx * s;
				double pendy = py + pdy * s;
				
				context.LineTo(pendx, pendy);
				context.Stroke();
				
				if (d_drawArrowHead)
				{
					context.MoveTo(px + pdx, py + pdy);
					context.LineTo(pendx - 0.5 * pdy * (1 - s), pendy + 0.5 * pdx * (1 - s));
					context.LineTo(pendx + 0.5 * pdy * (1 - s), pendy - 0.5 * pdx * (1 - s));
					context.ClosePath();

					//context.LineTo(ppx - l * (pdx - pdy * 0.5), ppy - l * (pdy + pdx * 0.5));
					//context.ClosePath();
				
					if (MarkerStyle == MarkerType.FilledCircle ||
					    MarkerStyle == MarkerType.FilledSquare ||
					    MarkerStyle == MarkerType.FilledTriangle)
					{
						context.Fill();
					}
					else
					{
						context.Stroke();
					}
				}
			}
		}
	}
}

