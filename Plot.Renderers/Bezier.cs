using System;
using Biorob.Math.Interpolation;
using System.Collections.Generic;
using Biorob.Math;
using Biorob.Math.Functions;
using System.Linq;
namespace Plot.Renderers
{
	public class Bezier : Line
	{
		private Range d_periodic;
		private Biorob.Math.Functions.Bezier d_bezier;
		private PiecewisePolynomial d_polynomial;
		
		public Bezier(PiecewisePolynomial polynomial, Color color, string label) : base(color, label)
		{
			SetPiecewisePolynomial(polynomial);
		}
		
		private void SetPiecewisePolynomial(PiecewisePolynomial polynomial)
		{
			d_polynomial = polynomial;
			
			if (d_polynomial != null)
			{
				d_bezier = new Biorob.Math.Functions.Bezier(d_polynomial);
				
				List<Point> pts = new List<Point>();
				
				foreach (Biorob.Math.Functions.PiecewisePolynomial.Piece piece in polynomial.Pieces)
				{
					pts.Add(new Point(piece.Begin, piece.Coefficients[piece.Coefficients.Length - 1]));
				}
				
				if (polynomial.Count > 0)
				{
					Biorob.Math.Functions.PiecewisePolynomial.Piece piece = polynomial[polynomial.Count - 1];
					pts.Add(new Point(piece.End, piece.Coefficients.Sum()));
				}
				
				Data = pts;
			}
			else
			{
				d_bezier = null;
				Data = new List<Point>();
			}
		}

		public Bezier(PiecewisePolynomial poly, Color color) : this(poly, color, null)
		{
		}
		
		public Bezier(PiecewisePolynomial poly) : this(poly, null)
		{
		}
		
		public Bezier(Color color, string name) : this(null, color, name)
		{
		}
		
		public Bezier(string name) : this(null, null, name)
		{
		}
		
		public Bezier() : this(null, null, null)
		{
		}
		
		protected virtual Bezier CopyTo(Bezier other)
		{
			base.CopyTo(other);
			
			if (d_polynomial != null)
			{
				other.PiecewisePolynomial = new PiecewisePolynomial(d_polynomial.Pieces);
			}
			
			return other;
		}
		
		public override Renderer Copy()
		{
			return CopyTo(new Bezier());
		}
		
		public PiecewisePolynomial PiecewisePolynomial
		{
			get
			{
				return d_polynomial;
			}
			set
			{
				SetPiecewisePolynomial(value);
			}
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
				
				EmitChanged();
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

		private void RenderBezier(Cairo.Context context, Point scale)
		{			
			bool first = true;
			bool wasoutside = true;

			context.Save();
			
			/* We are going to render this stuff now */
			foreach (Biorob.Math.Functions.Bezier.Piece piece in d_bezier)
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

		public override void Render(Cairo.Context context, Point scale)
		{
			// First render our interpolated line
			RenderBezier(context, scale);
			
			// Then render the markers, if needed
			RenderMarkers(context, scale, 0, Count);
		}
		
		private void OnPeriodicChanged(object source, EventArgs args)
		{
			EmitChanged();
		}
		
		public override Point ValueAtX(double x, out bool interpolated, out bool extrapolated)
		{
			interpolated = false;
			extrapolated = false;
			
			if (d_bezier == null)
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

