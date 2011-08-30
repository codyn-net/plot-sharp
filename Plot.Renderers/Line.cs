using System;
using System.Collections.Generic;

namespace Plot.Renderers
{
	public class Line : Renderer, ILabeled, IColored
	{
		public enum MarkerType
		{
			None,
			Circle,
			Square,
			Triangle,
			Cross,
			FilledCircle,
			FilledSquare,
			FilledTriangle,
		}

		private List<Point<double>> d_data;
		private SortedList<Point<double>> d_sortedData;

		private Color d_color;
		private string d_label;
		private int d_unprocessed;
		private int d_lineWidth;
		private bool d_isMarkup;
		
		private double d_markerSize;
		private MarkerType d_markerStyle;
		private bool d_showLine;
		private static Point<double> s_triangleDxDy;
		private static IComparer<Point<double>> s_pointComparer;
		
		private delegate void MarkerRenderFunc(Cairo.Context context, Point<double> scale, Point<double> item, int idx);
		private MarkerRenderFunc d_markerRenderer;
		
		static Line()
		{
			s_triangleDxDy = new Point<double>(Math.Sin(1 / 3.0 * Math.PI),
			                                   Math.Cos(1 / 3.0 * Math.PI));	
			
			s_pointComparer = new PointComparer();
		}
		
		public Line(IEnumerable<Point<double>> data, Color color, string label)
		{
			d_data = new List<Point<double>>(data);
			d_label = label;
			d_lineWidth = 1;
			
			d_unprocessed = 0;
			
			d_sortedData = null;
			
			d_markerStyle = MarkerType.None;
			d_markerSize = 5;
			d_showLine = true;
			
			UpdateRanges();
			
			Color = d_color;
		}

		private void HandleColorChanged(object sender, EventArgs e)
		{
			EmitChanged();
		}
		
		public Line(IEnumerable<Point<double>> data, Color color) : this(data, color, "")
		{
		}
		
		public Line(IEnumerable<Point<double>> data) : this(data, null, "")
		{
		}
		
		public Line(Color color, string name) : this(new Point<double>[] {}, color, name)
		{
		}
		
		public Line(string name) : this(null, name)
		{
		}
		
		public Line() : this("")
		{
		}

		public MarkerType MarkerStyle
		{
			get
			{
				return d_markerStyle;
			}
			set
			{
				if (d_markerStyle == value)
				{
					return;
				}

				d_markerStyle = value;
				
				switch (d_markerStyle)
				{
					case MarkerType.Circle:
						d_markerRenderer = RenderCircle;
					break;
					case MarkerType.FilledCircle:
						d_markerRenderer = RenderFilledCircle;
					break;
					case MarkerType.FilledSquare:
						d_markerRenderer = RenderFilledSquare;
					break;
					case MarkerType.Square:
						d_markerRenderer = RenderSquare;
					break;
					case MarkerType.Cross:
						d_markerRenderer = RenderCross;
					break;
					case MarkerType.Triangle:
						d_markerRenderer = RenderTriangle;
					break;
					case MarkerType.FilledTriangle:
						d_markerRenderer = RenderFilledTriangle;
					break;
					case MarkerType.None:
						d_markerRenderer = null;
					break;
					default:
						throw new NotImplementedException();
				}

				EmitChanged();
			}
		}

		public double MarkerSize
		{
			get
			{
				return d_markerSize;
			}
			set
			{
				if (d_markerSize != value)
				{
					d_markerSize = value;
				}

				EmitChanged();
			}
		}

		protected virtual Line CopyTo(Line other)
		{
			base.CopyTo(other);

			other.d_label = d_label;
			
			if (d_color != null)
			{
				other.d_color = new Color(d_color.R, d_color.G, d_color.B, d_color.A);
			}
			
			List<Point<double>> data = new List<Point<double>>();
			
			foreach (Point<double> pt in d_data)
			{
				data.Add(new Point<double>(pt.X, pt.Y));
			}
			
			other.Data = data;
			other.LineWidth = d_lineWidth;
			other.XRange.Update(XRange);
			other.YRange.Update(YRange);
			other.d_unprocessed = d_unprocessed;
			
			other.d_markerSize = d_markerSize;
			other.MarkerStyle = d_markerStyle;
			other.d_showLine = d_showLine;
			
			return other;
		}
		
		public override Renderer Copy()
		{
			return CopyTo(new Line());
		}
		
		public override bool CanRule
		{
			get
			{
				return Chronological;
			}
		}
		
		public bool Chronological
		{
			get
			{
				return d_sortedData == null;
			}
		}
		
		private void UpdateRanges()
		{
			XRange.Freeze();
			YRange.Freeze();
			
			XRange.Update(0, 0);
			YRange.Update(0, 0);
			
			d_sortedData = null;
			bool makesorted = false;
			
			for (int i = 0; i < d_data.Count; ++i)
			{
				Point<double> p = d_data[i];

				if (i == 0 || p.X < XRange.Min)
				{
					XRange.Min = p.X;
				}
				
				if (i == 0 || p.X > XRange.Max)
				{
					XRange.Max = p.X;
				}
				
				if (i == 0 || p.Y < YRange.Min)
				{
					YRange.Min = p.Y;
				}
				
				if (i == 0 || p.Y > YRange.Max)
				{
					YRange.Max = p.Y;
				}
				
				if (makesorted)
				{
					d_sortedData.Add(p);
				}
				
				if (i != 0 && !makesorted && d_data[i - 1].X > p.X)
				{
					makesorted = true;
					
					d_sortedData = new SortedList<Point<double>>(new PointComparer());
					
					for (int j = 0; j <= i; ++j)
					{
						d_sortedData.Add(d_data[i]);
					}
				}				
			}
			
			XRange.Thaw();
			YRange.Thaw();
		}
		
		public int LineWidth
		{
			get
			{
				return d_lineWidth;
			}
			set
			{
				d_lineWidth = value;
				EmitChanged();
			}
		}
		
		public IEnumerable<Point<double>> Data
		{
			get
			{
				return d_data;
			}
			set
			{
				d_data.Clear();
				d_data.AddRange(value);
				
				UpdateRanges();
				
				EmitChanged();
			}
		}
		
		public IEnumerable<Point<double>> Range(int start)
		{
			return Range(start, d_data.Count - start);
		}
		
		public IEnumerable<Point<double>> Range(int start, int length)
		{
			int end = Math.Min(start + length, d_data.Count);
			start = Math.Max(start, 0);

			for (int i = start; i < end; ++i)
			{
				yield return d_data[i];
			}
		}
		
		public int Count
		{
			get
			{
				return d_data.Count;
			}
		}

		public Color Color
		{
			get
			{
				return d_color;
			}
			set
			{
				if (value == null)
				{
					if (d_color != null)
					{
						d_color.Changed -= HandleColorChanged;
					}

					d_color = null;
				}
				else
				{
					if (d_color == null)
					{
						d_color = value.Copy();
						d_color.Changed += HandleColorChanged;
					}
					else
					{
						d_color.Update(value);
					}
				}
			}
		}
		
		protected List<Point<double>> PrivateData
		{
			get
			{
				return d_data;
			}
		}
		
		public string LabelMarkup
		{
			get
			{
				return d_isMarkup ? d_label : null;
			}
			set
			{
				d_label = value;
				d_isMarkup = true;

				EmitChanged();
			}
		}
		
		public string Label
		{
			get
			{
				return !d_isMarkup ? d_label : null;
			}
			set
			{
				d_label = value;
				d_isMarkup = false;

				EmitChanged();
			}
		}
		
		public void Append(Point<double> pt)
		{
			d_data.Add(pt);
			
			if (d_sortedData != null)
			{
				d_sortedData.Add(pt);
			}
			else if (d_data.Count > 1 && d_data[d_data.Count - 2].X > pt.X)
			{
				d_sortedData = new SortedList<Point<double>>(new PointComparer());
				
				foreach (Point<double> item in d_data)
				{
					d_sortedData.Add(item);
				}
			}

			++d_unprocessed;
		}
		
		public void Processed()
		{
			for (int i = d_data.Count - d_unprocessed; i < d_data.Count; ++i)
			{
				
			}

			d_unprocessed = 0;
		}
		
		public int Unprocessed
		{
			get
			{
				return d_unprocessed;
			}
		}
		
		public bool HasData(int idx)
		{
			if (idx < 0)
			{
				idx = d_data.Count + idx;
			}
			
			return idx < d_data.Count;
		}
		
		public Point<double> this[int idx]
		{
			get
			{
				if (idx < 0)
				{
					idx = d_data.Count + idx;
				}
				
				if (idx < 0 || idx >= d_data.Count)
				{
					return new Point<double>();
				}
				else
				{
					return d_data[idx];
				}
			}
		}

		public bool ShowLine
		{
			get
			{
				return d_showLine;
			}
			set
			{
				if (d_showLine != value)
				{
					d_showLine = value;
					EmitChanged();
				}
			}
		}
		
		private void MakeCircle(Cairo.Context context, Point<double> scale, Point<double> item, int idx)
		{
			context.Arc(item.X * scale.X, item.Y * scale.Y, (d_markerSize - d_lineWidth) / 2, 0, 2 * Math.PI);
		}

		private void RenderCircle(Cairo.Context context, Point<double> scale, Point<double> item, int idx)
		{
			MakeCircle(context, scale, item, idx);
			context.Stroke();
		}
		
		private void RenderFilledCircle(Cairo.Context context, Point<double> scale, Point<double> item, int idx)
		{
			MakeCircle(context, scale, item, idx);
			context.FillPreserve();
			context.Stroke();
		}
		
		private void MakeSquare(Cairo.Context context, Point<double> scale, Point<double> item, int idx)
		{
			double size = d_markerSize - d_lineWidth;

			context.MoveTo(item.X * scale.X, item.Y * scale.Y);
			context.RelMoveTo(-size / 2, -size /  2);
			context.RelLineTo(size, 0);
			context.RelLineTo(0, size);
			context.RelLineTo(-size, 0);
			context.ClosePath();
		}
		
		private void RenderSquare(Cairo.Context context, Point<double> scale, Point<double> item, int idx)
		{
			MakeSquare(context, scale, item, idx);
			context.Stroke();
		}
		
		private void RenderFilledSquare(Cairo.Context context, Point<double> scale, Point<double> item, int idx)
		{
			MakeSquare(context, scale, item, idx);
			context.FillPreserve();
			context.Stroke();
		}
		
		private void MakeTriangle(Cairo.Context context, Point<double> scale, Point<double> item, int idx)
		{
			double halfsize = (d_markerSize - d_lineWidth) / 2;

			context.MoveTo(item.X * scale.X, item.Y * scale.Y);
			context.RelMoveTo(0, halfsize);
			
			double dx = halfsize * s_triangleDxDy.X;
			double dy = halfsize * s_triangleDxDy.Y;
			
			context.RelLineTo(dx, -halfsize - dy);
			context.RelLineTo(2 * -dx, 0);
			context.ClosePath();
		}

		private void RenderTriangle(Cairo.Context context, Point<double> scale, Point<double> item, int idx)
		{
			MakeTriangle(context, scale, item, idx);
			context.Stroke();
		}
		
		private void RenderFilledTriangle(Cairo.Context context, Point<double> scale, Point<double> item, int idx)
		{
			MakeTriangle(context, scale, item, idx);
			
			context.FillPreserve();
			context.Stroke();
		}

		private void RenderCross(Cairo.Context context, Point<double> scale, Point<double> item, int idx)
		{
			context.MoveTo(item.X * scale.X, item.Y * scale.Y);
			context.RelMoveTo(-d_markerSize / 2, -d_markerSize / 2);
			context.RelLineTo(d_markerSize, d_markerSize);
			context.Stroke();
			
			context.MoveTo(item.X * scale.X, item.Y * scale.Y);
			context.RelMoveTo(d_markerSize / 2, -d_markerSize / 2);
			context.RelLineTo(-d_markerSize, d_markerSize);
			context.Stroke();
		}
		
		private void RenderMarkers(Cairo.Context context, Point<double> scale, int idx, int length)
		{
			d_color.Set(context);
			context.LineWidth = LineWidth;

			int curidx = idx;
			
			foreach (Point<double> item in Range(idx, length))
			{
				d_markerRenderer(context, scale, item, curidx);
				curidx++;
			}
		}
		
		private void RenderLine(Cairo.Context context, Point<double> scale, int idx, int length)
		{
			bool first = true;
			
			d_color.Set(context);
			context.LineWidth = d_lineWidth;
			
			foreach (Point<double> item in Range(idx, length))
			{
				if (first)
				{
					context.MoveTo(item.X * scale.X, item.Y * scale.Y);
					first = false;
				}
				else
				{
					context.LineTo(item.X * scale.X, item.Y * scale.Y);
				}
			}

			context.Stroke();
		}
		
		public override void Render(Cairo.Context context, Point<double> scale)
		{
			if (d_showLine)
			{
				RenderLine(context, scale, 0, d_data.Count);
			}
			
			if (d_markerStyle != MarkerType.None && d_markerSize > 0)
			{
				RenderMarkers(context, scale, 0, d_data.Count);
			}
		}
		
		private class PointComparer : IComparer<Point<double>>
		{		
			public int Compare(Point<double> a, Point<double> b)
			{
				return a.X < b.X ? -1 : (a.X > b.X ? 1 : 0);
			}
		}
		
		public override Point<double> ValueClosestToX(double x)
		{
			bool interpolated;
			bool extrapolated;
			
			return ValueAtX(x, false, out interpolated, out extrapolated);
		}
		
		public override Point<double> ValueAtX(double x, out bool interpolated, out bool extrapolated)
		{
			return ValueAtX(x, true, out interpolated, out extrapolated);
		}
		
		private Point<double> ValueAtX(double x, bool dointerp, out bool interpolated, out bool extrapolated)
		{
			extrapolated = false;
			interpolated = false;
			
			int idx;
			Point<double> sp = new Point<double>(x, 0);
			
			List<Point<double>> data;
			
			if (d_sortedData != null)
			{
				data = d_sortedData;
			}			
			else
			{
				data = d_data;
			}
			
			if (data.Count == 0)
			{
				extrapolated = true;
				return new Point<double>(0, 0);
			}
			
			idx = data.BinarySearch(0, data.Count, sp, s_pointComparer);
			
			if (!dointerp)
			{
				// After data
				if (idx >= 0)
				{
					// In data
					return data[idx].Copy();
				}
				else
				{
					idx = ~idx;
					
					if (idx == data.Count)
					{
						return data[data.Count - 1].Copy();
					}
					else if (idx == 0)
					{
						return data[idx].Copy();
					}
					
					/* In between idx - 1 and idx */
					double df = (data[idx].X - data[idx - 1].X) / 2;
					
					if (x - data[idx - 1].X <= df)
					{
						return data[idx - 1].Copy();
					}
					else
					{
						return data[idx].Copy();
					}
				}
			}
			else if (idx < 0)
			{
				idx = ~idx;
				
				if (idx == 0 || idx == data.Count)
				{
					extrapolated = true;
				}
				else
				{
					interpolated = true;
				}
			}
			
			int fidx = idx > 0 ? idx - 1 : 0;
			int sidx = idx < data.Count ? idx : data.Count - 1;
			
			double ret;
			
			if (fidx >= data.Count || sidx >= data.Count)
			{
				ret = data[data.Count - 1].Y;
			}
			else
			{
				Point<double> ps = data[sidx];
				Point<double> pf = data[fidx];

				double factor = ps.X == pf.X ? 1 : (ps.X - x) / (ps.X - pf.X);
				ret = pf.Y * factor + (ps.Y * (1 - factor));
			}
			
			return new Point<double>(x, ret);
		}
		
		public double[] Sample(double[] sites)
		{
			double[] ret = new double[sites.Length];
			
			for (int i = 0; i < sites.Length; ++i)
			{
				ret[i] = ValueAtX(sites[i]).Y;
			}
			
			return ret;
		}
	}
}

