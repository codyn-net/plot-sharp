using System;
using System.Collections.Generic;
using Biorob.Math;

namespace Plot.Renderers
{
	public class Line : Renderer, ILabeled, IColored
	{
		private List<Point> d_data;
		private SortedList<Point> d_sortedData;

		private Color d_color;
		private string d_ylabel;
		private string d_xlabel;
		private int d_unprocessed;
		private int d_lineWidth;
		private bool d_isYMarkup;
		private bool d_isXMarkup;
		
		private double d_markerSize;
		private MarkerStyle d_markerStyle;

		private static IComparer<Point> s_pointComparer;
		
		private LineStyle d_lineStyle;
		
		private Marker.Renderer d_markerRenderer;
		
		static Line()
		{
			s_pointComparer = new PointComparer();
		}
		
		public Line()
		{
			d_data = new List<Point>();
			
			d_lineWidth = 1;
			d_unprocessed = 0;
			
			d_sortedData = null;
			
			d_markerStyle = MarkerStyle.None;
			d_markerSize = 5;
			
			d_lineStyle = LineStyle.Single;
			
			UpdateRanges();
		}
		
		private void HandleColorChanged(object sender, EventArgs e)
		{
			EmitChanged();
		}

		public LineStyle LineStyle
		{
			get
			{
				return d_lineStyle;
			}
			set
			{
				if (d_lineStyle != value)
				{
					d_lineStyle = value;
					EmitChanged();
				}
			}
		}

		public MarkerStyle MarkerStyle
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
				d_markerRenderer = Marker.Lookup(d_markerStyle);

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

			other.d_ylabel = d_ylabel;
			other.d_xlabel = d_xlabel;
			
			other.d_isYMarkup = d_isYMarkup;
			other.d_isXMarkup = d_isXMarkup;
			
			if (d_color != null)
			{
				other.d_color = new Color(d_color.R, d_color.G, d_color.B, d_color.A);
			}
			
			List<Point> data = new List<Point>();
			
			foreach (Point pt in d_data)
			{
				data.Add(new Point(pt.X, pt.Y));
			}
			
			other.Data = data;
			other.LineWidth = d_lineWidth;
			other.XRange.Update(XRange);
			other.YRange.Update(YRange);
			other.d_unprocessed = d_unprocessed;
			
			other.d_markerSize = d_markerSize;
			other.MarkerStyle = d_markerStyle;
			other.d_lineStyle = d_lineStyle;
			
			return other;
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
				Point p = d_data[i];

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
					
					d_sortedData = new SortedList<Point>(new PointComparer());
					
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
		
		public IEnumerable<Point> SortedData
		{
			get
			{
				if (d_sortedData != null)
				{
					return d_sortedData;
				}
				else
				{
					return d_data;
				}
			}
		}
		
		public IEnumerable<Point> Data
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
		
		public delegate Point DataGenerator(double x);
		
		public void GenerateData(Range xrange, int samples, DataGenerator generator)
		{
			List<Point> data = new List<Point>(samples);

			if (samples == 0)
			{
				Data = data;
				return;
			}
			
			if (samples == 1)
			{
				data.Add(generator(xrange.Min));
				return;
			}
			
			double spend = xrange.Min;
			
			for (int i = 0; i < samples; ++i)
			{
				data.Add(generator(spend));

				int div = samples - i - 1;
				
				if (div > 0)
				{
					double df = (xrange.Max - spend) / (samples - i - 1);
					spend += df;
				}
			}
			
			Data = data;
		}
		
		public IEnumerable<Point> Range(int start)
		{
			return Range(start, d_data.Count - start);
		}
		
		public IEnumerable<Point> Range(int start, int length)
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
		
		protected List<Point> PrivateData
		{
			get
			{
				return d_data;
			}
		}
		
		public string YLabelMarkup
		{
			get { return d_isYMarkup ? d_ylabel : null; }
			set
			{
				d_ylabel = value;
				d_isYMarkup = true;

				EmitChanged();
			}
		}
		
		public string YLabel
		{
			get { return !d_isYMarkup ? d_ylabel : null; }
			set
			{
				d_ylabel = value;
				d_isYMarkup = false;

				EmitChanged();
			}
		}
		
		public string XLabelMarkup
		{
			get { return d_isXMarkup ? d_xlabel : null; }
			set
			{
				d_xlabel = value;
				d_isXMarkup = true;

				EmitChanged();
			}
		}
		
		public string XLabel
		{
			get { return !d_isXMarkup ? d_xlabel : null; }
			set
			{
				d_xlabel = value;
				d_isXMarkup = false;

				EmitChanged();
			}
		}
		
		public void Append(Point pt)
		{
			d_data.Add(pt);
			
			if (d_sortedData != null)
			{
				d_sortedData.Add(pt);
			}
			else if (d_data.Count > 1 && d_data[d_data.Count - 2].X > pt.X)
			{
				d_sortedData = new SortedList<Point>(new PointComparer());
				
				foreach (Point item in d_data)
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
		
		public Point this[int idx]
		{
			get
			{
				if (idx < 0)
				{
					idx = d_data.Count + idx;
				}
				
				if (idx < 0 || idx >= d_data.Count)
				{
					return new Point();
				}
				else
				{
					return d_data[idx];
				}
			}
		}
		

		
		protected void RenderMarkers(Cairo.Context context, Point scale, int idx, int length)
		{
			if (d_markerRenderer == null)
			{
				return;
			}
			
			context.Save();
			
			if (d_color != null)
			{
				d_color.Set(context);
			}

			context.LineWidth = LineWidth;

			int curidx = idx;
			
			foreach (Point item in Range(idx, length))
			{
				d_markerRenderer(context, scale, item, d_markerSize, d_lineWidth);
				curidx++;
			}
			
			context.Restore();
		}
		
		public static void SetLineStyle(Cairo.Context context, double lw, LineStyle style)
		{
			context.LineWidth = lw;

			switch (style)
			{
				case LineStyle.Single:
					context.SetDash(new double[] {}, 0);
				break;
				case LineStyle.Dotted:
					context.SetDash(new double[] {lw, lw * 2}, 0);
				break;
				case LineStyle.Dashed:
					context.SetDash(new double[] {lw * 4, lw * 4}, 0);
				break;
			}
		}
		
		protected void SetLineStyle(Cairo.Context context)
		{
			if (d_color != null)
			{
				d_color.Set(context);
			}

			SetLineStyle(context, d_lineWidth, d_lineStyle);
		}
		
		protected virtual void CreatePath(Cairo.Context context, Point scale, int idx, int length)
		{
			bool first = true;

			foreach (Point item in Range(idx, length))
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
		}
		
		protected void RenderLine(Cairo.Context context, Point scale, int idx, int length)
		{
			context.Save();
			
			SetLineStyle(context);
			
			CreatePath(context, scale, idx, length);

			context.Stroke();
			context.Restore();
		}
		
		public override void Render(Cairo.Context context, Point scale)
		{
			if (d_lineStyle != LineStyle.None)
			{
				RenderLine(context, scale, 0, d_data.Count);
			}
			
			if (d_markerStyle != MarkerStyle.None && d_markerSize > 0)
			{
				RenderMarkers(context, scale, 0, d_data.Count);
			}
		}
		
		private class PointComparer : IComparer<Point>
		{		
			public int Compare(Point a, Point b)
			{
				return a.X < b.X ? -1 : (a.X > b.X ? 1 : 0);
			}
		}
		
		public override Point ValueClosestToX(double x)
		{
			bool interpolated;
			bool extrapolated;
			
			return ValueAtX(x, false, out interpolated, out extrapolated);
		}
		
		public override Point ValueAtX(double x, out bool interpolated, out bool extrapolated)
		{
			return ValueAtX(x, true, out interpolated, out extrapolated);
		}
		
		private Point ValueAtX(double x, bool dointerp, out bool interpolated, out bool extrapolated)
		{
			extrapolated = false;
			interpolated = false;
			
			int idx;
			Point sp = new Point(x, 0);
			
			List<Point> data;
			
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
				return new Point(0, 0);
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
				Point ps = data[sidx];
				Point pf = data[fidx];

				double factor = ps.X == pf.X ? 1 : (ps.X - x) / (ps.X - pf.X);
				ret = pf.Y * factor + (ps.Y * (1 - factor));
			}
			
			return new Point(x, ret);
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

