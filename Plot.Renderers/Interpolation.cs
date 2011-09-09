using System;
using Biorob.Math.Interpolation;
using System.Collections.Generic;
using Biorob.Math;
using Biorob.Math.Functions;

namespace Plot.Renderers
{
	public class Interpolation : Line
	{
		private Range d_periodic;
		private PiecewisePolynomial d_polynomial;
		private Bezier d_bezier;
		
		public Interpolation(IEnumerable<Point> data, Color color, string label) : base(data, color, label)
		{
		}

		public Interpolation(IEnumerable<Point> data, Color color) : base(data, color)
		{
		}
		
		public Interpolation(IEnumerable<Point> data) : base(data)
		{
		}
		
		public Interpolation(Color color, string name) : base(color, name)
		{
		}
		
		public Interpolation(string name) : base(name)
		{
		}
		
		public Interpolation() : base()
		{
		}
		
		public PiecewisePolynomial PiecewisePolynomial
		{
			get
			{
				return d_polynomial;
			}
		}
		
		public Bezier Bezier
		{
			get
			{
				return d_bezier;
			}
		}

		private void RecalculateCoefficients()
		{
			d_polynomial = null;
			d_bezier = null;
			
			List<Point> pts = new List<Point>(SortedData);
					
			if (d_periodic != null)
			{
				Biorob.Math.Interpolation.Periodic.Extend(pts, d_periodic.Min, d_periodic.Max);
			}
			
			if (pts.Count < 2)
			{
				return;
			}
			
			PChip pchip = new PChip();
			
			d_polynomial = pchip.InterpolateSorted(pts);
			d_bezier = new Bezier(d_polynomial);
			
			XRange.Update(d_polynomial.XRange);
			YRange.Update(d_polynomial.YRange);
		}
		
		public Range Periodic
		{
			get
			{
				return d_periodic;
			}
			set
			{
				if (d_periodic == value)
				{
					return;
				}

				if (d_periodic != null)
				{
					d_periodic.Changed -= OnPeriodicChanged;
				}
				
				d_periodic = value;
				
				if (d_periodic != null)
				{
					d_periodic.Changed += OnPeriodicChanged;
				}
				
				RecalculateCoefficients();
			}
		}
		
		private void SetOutsideLineStyle(Cairo.Context context)
		{
			Color nc = Color.Copy();
			nc.A *= 0.8;
			
			nc.Set(context);
			context.SetDash(new double[] {LineWidth, LineWidth * 4}, 0);
		}
		
		private void StyleAccordingToOutside(Cairo.Context context, bool outside)
		{
			if (outside)
			{
				SetOutsideLineStyle(context);
			}
			else
			{
				SetLineStyle(context);
			}
		}

		private void RenderInterpolated(Cairo.Context context, Point scale)
		{
			if (d_bezier == null)
			{
				return;
			}
			
			bool first = true;
			bool wasoutside = true;

			context.Save();
			
			/* We are going to render this stuff now */
			foreach (Bezier.Piece piece in d_bezier)
			{
				bool isoutside = (d_periodic != null && (piece.Begin.X < d_periodic.Min || piece.End.X > d_periodic.Max));
				
				if (wasoutside != isoutside)
				{
					StyleAccordingToOutside(context, wasoutside);

					if (!first)
					{
						context.Stroke();
						first = true;
					}
				}
				
				if (first)
				{
					context.MoveTo(piece.Begin.X * scale.X, piece.Begin.Y * scale.Y);
					first = false;
				}

				context.CurveTo(piece.C1.X * scale.X, piece.C1.Y * scale.Y,
				                piece.C2.X * scale.X, piece.C2.Y * scale.Y,
				                piece.End.X * scale.X, piece.End.Y * scale.Y);
			
				wasoutside = isoutside;
			}
			
			if (!first)
			{
				StyleAccordingToOutside(context, wasoutside);
				context.Stroke();
			}

			context.Restore();
		}
		
		public override void EmitChanged()
		{
			RecalculateCoefficients();
			base.EmitChanged();
		}

		public override void Render(Cairo.Context context, Point scale)
		{
			// First render our interpolated line
			RenderInterpolated(context, scale);
			
			// Then render the markers, if needed
			RenderMarkers(context, scale, 0, Count);
		}
		
		private void OnPeriodicChanged(object source, EventArgs args)
		{
			RecalculateCoefficients();
		}
		
		public override Point ValueAtX(double x, out bool interpolated, out bool extrapolated)
		{
			interpolated = false;
			extrapolated = false;
			
			if (d_polynomial == null)
			{
				extrapolated = true;
				return new Point(0, 0);
			}
			
			Range xrange = d_polynomial.XRange;
			
			if (x < xrange.Min)
			{
				extrapolated = true;
				return new Point(xrange.Min, d_polynomial[0].Begin);
			}
			else if (x > xrange.Max)
			{
				extrapolated = true;
				return new Point(xrange.Max, d_polynomial[d_polynomial.Count - 1].End);
			}

			if (d_periodic != null && (x < d_periodic.Min || x > d_periodic.Max))
			{
				extrapolated = true;
			}
			
			// Do the interpolation
			PiecewisePolynomial.Piece piece = d_polynomial.PieceAt(x);
			
			if (x != piece.Begin || x != piece.End)
			{
				interpolated = true;
			}

			return new Point(x, piece.Evaluate(x));
		}
	}
}

