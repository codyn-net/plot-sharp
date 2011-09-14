using System;
using System.Collections.Generic;
using Biorob.Math;

namespace Plot.Renderers
{
	public class Vector : Line
	{
		public enum LengthType
		{
			Axis,
			Pixel
		}
		
		private List<double> d_cosAlpha;
		private List<double> d_sinAlpha;

		private List<double> d_alpha;
		private List<double> d_length;
		private List<double> d_lengthNorm;
		private double d_equalLength;
		private double d_pixelLength;
		private LengthType d_lengthType;
		private Point d_scale;
		private bool d_drawArrowHead;
		private double d_arrowHeadSize;

		public Vector()
		{
			d_alpha = new List<double>();
			d_cosAlpha = new List<double>();
			d_sinAlpha = new List<double>();
			d_length = new List<double>();
			d_lengthNorm = new List<double>();
			d_equalLength = 10;
			d_pixelLength = 0;
			d_lengthType = LengthType.Axis;
			d_drawArrowHead = true;
			d_arrowHeadSize = -1;
			
			LineStyle = LineStyle.None;
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
			 v.d_scale = new Point(d_scale.X, d_scale.Y);
			 v.d_drawArrowHead = d_drawArrowHead;
			 v.d_arrowHeadSize = d_arrowHeadSize;
			 v.d_lengthType = d_lengthType;
			 
			 v.d_alpha = new List<double>(d_alpha);
			 v.d_cosAlpha = new List<double>(d_cosAlpha);
			 v.d_sinAlpha = new List<double>(d_sinAlpha);

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
		
		public IEnumerable<double> Alpha
		{
			get
			{
				return d_alpha;
			}
			set
			{
				d_alpha.Clear();
				d_alpha.AddRange(value);
				
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
			d_cosAlpha = new List<double>(d_alpha.Count);
			d_sinAlpha = new List<double>(d_alpha.Count);
			
			foreach (double alpha in d_alpha)
			{
				d_cosAlpha.Add(System.Math.Cos(alpha));
				d_sinAlpha.Add(System.Math.Sin(alpha));
			}

			if (d_lengthType == LengthType.Pixel)
			{
				/* Do that later when we know the scale */
				d_scale = null;
				return;
			}

			d_lengthNorm.Clear();

			for (int i = 0; i < d_alpha.Count; ++i)
			{
				double l = d_equalLength;
				
				if (i < d_length.Count)
				{
					l = d_length[i];
				}

				d_lengthNorm.Add(l);
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
		
		private bool SameScale(Point scale)
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
			
			double sql = d_pixelLength * d_pixelLength;

			for (int i = 0; i < d_alpha.Count; ++i)
			{
				double l = sql;
				
				if (i < d_length.Count)
				{
					l = d_length[i] * d_length[i];
				}

				d_lengthNorm.Add(Math.Sqrt(l / (d_scale.X * d_scale.X * d_cosAlpha[i] * d_cosAlpha[i] + d_scale.Y * d_scale.Y * d_sinAlpha[i] * d_sinAlpha[i])));
			}
		}
		
		public override void Render(Cairo.Context context, Point scale)
		{
			base.Render(context, scale);
			
			int idx = 0;
			int length = Count;
			
			int endidx;
			
			endidx = Math.Min(idx + length, d_alpha.Count);
			
			context.SetSourceRGB(Color.R, Color.G, Color.B);
			context.LineWidth = LineWidth;
			
			if (d_lengthType == LengthType.Pixel && !SameScale(scale))
			{
				d_scale = scale.Copy();
				RecalculatePixelLength();
			}
			
			for (int i = idx; i < endidx; ++i)
			{
				Point item = PrivateData[i];
				
				// In pixel space
				double px = scale.X * item.X;
				double py = scale.Y * item.Y;
	
				// dx, dy from px, py to where to draw the line
				double pdx = d_lengthNorm[i] * scale.X * d_cosAlpha[i];
				double pdy = d_lengthNorm[i] * scale.Y * d_sinAlpha[i];
				
				double l = Math.Sqrt(pdx * pdx + pdy * pdy);
				
				if (MarkerStyle == MarkerStyle.Circle)
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
				
					if (MarkerStyle == MarkerStyle.FilledCircle ||
					    MarkerStyle == MarkerStyle.FilledSquare ||
					    MarkerStyle == MarkerStyle.FilledTriangle)
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

