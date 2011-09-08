using System;
using Biorob.Math.Interpolation;
using System.Collections.Generic;

namespace Plot.Renderers
{
	public class Interpolation : Line
	{
		private Range<double> d_periodic;
		private List<PChip.Piece> d_pieces;
		private List<Point<double>[]> d_curves;
		
		public Interpolation(IEnumerable<Point<double>> data, Color color, string label) : base(data, color, label)
		{
		}

		public Interpolation(IEnumerable<Point<double>> data, Color color) : base(data, color)
		{
		}
		
		public Interpolation(IEnumerable<Point<double>> data) : base(data)
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
		
		public IEnumerable<PChip.Piece> Pieces
		{
			get
			{
				return d_pieces;
			}
		}
		
		public IEnumerable<Point<double>[]> Curves
		{
			get
			{
				return d_curves;
			}
		}

		private Point<double>[] PieceToCurve(PChip.Piece piece)
		{
			Point<double>[] ret = new Point<double>[4];
			
			double dx = piece.P1.X - piece.P0.X;
					
			// Convert from polynomial form to bezier curve form			
			ret[0] = new Point<double>(piece.P0.X,
			                           piece.P0.Y);
			
			ret[1] = new Point<double>(piece.P0.X + dx / 3,
			                           piece.P0.Y + piece.M0 / 3);
			
			ret[2] = new Point<double>(piece.P1.X - dx / 3,
			                           piece.P1.Y - piece.M1 / 3);
			
			ret[3] = new Point<double>(piece.P1.X, piece.P1.Y);
			
			return ret;
		}

		private void RecalculateCoefficients()
		{
			d_pieces = null;
			d_curves = null;
			
			List<Biorob.Math.Interpolation.Point> pts = new List<Biorob.Math.Interpolation.Point>(Count);
			
			foreach (Point<double> pt in SortedData)
			{
				pts.Add(new Biorob.Math.Interpolation.Point(pt.X, pt.Y));
			}
					
			if (d_periodic != null)
			{
				Biorob.Math.Interpolation.Periodic.Extend(pts, d_periodic.Min, d_periodic.Max);
			}
			
			if (pts.Count < 2)
			{
				return;
			}
			
			d_pieces = PChip.InterpolateSorted(pts);
			
			d_curves = new List<Point<double>[]>();
			
			foreach (PChip.Piece piece in d_pieces)
			{
				d_curves.Add(PieceToCurve(piece));
			}
			
			XRange.Update(d_pieces[0].Start, d_pieces[d_pieces.Count - 1].End);			
		}
		
		public Range<double> Periodic
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

		private void RenderInterpolated(Cairo.Context context, Point<double> scale)
		{
			if (d_curves == null)
			{
				return;
			}
			
			bool first = true;
			bool wasoutside = true;

			context.Save();
			
			/* We are going to render this stuff now */
			foreach (Point<double>[] points in d_curves)
			{
				bool isoutside = (d_periodic != null && (points[0].X < d_periodic.Min || points[3].X > d_periodic.Max));
				
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
					context.MoveTo(points[0].X * scale.X, points[0].Y * scale.Y);
					first = false;
				}

				context.CurveTo(points[1].X * scale.X, points[1].Y * scale.Y,
				                points[2].X * scale.X, points[2].Y * scale.Y,
				                points[3].X * scale.X, points[3].Y * scale.Y);
			
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

		public override void Render(Cairo.Context context, Point<double> scale)
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
		
		public override Point<double> ValueAtX(double x, out bool interpolated, out bool extrapolated)
		{
			interpolated = false;
			extrapolated = false;
			
			if (d_curves == null)
			{
				extrapolated = true;
				return new Point<double>(0, 0);
			}
			
			double xmin = d_curves[0][0].X;
			double xmax = d_curves[d_curves.Count - 1][3].X;
			
			if (x < xmin)
			{
				extrapolated = true;
				return new Point<double>(xmin, d_curves[0][0].Y);
			}
			else if (x > xmax)
			{
				extrapolated = true;
				return new Point<double>(xmax, d_curves[d_curves.Count - 1][3].Y);
			}

			if (d_periodic != null && (x < d_periodic.Min || x > d_periodic.Max))
			{
				extrapolated = true;
			}
			
			// Do the interpolation
			foreach (Piece piece in d_pieces)
			{
				if (x >= piece.Start && x <= piece.End)
				{
					if (x != piece.Start || x != piece.End)
					{
						interpolated = true;
					}

					return new Point<double>(x, piece.Evaluate(x));
				}
			}
			
			return null;
		}
	}
}

