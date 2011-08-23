using System;
using System.Collections.Generic;

namespace Plot.Series
{
	public class Line : Changeable
	{
		private List<Point<double>> d_data;
		private SortedList<Point<double>> d_sortedData;

		private Color d_color;
		private string d_label;
		private int d_unprocessed;
		private int d_lineWidth;
		private Range<double> d_xrange;
		private Range<double> d_yrange;
		
		public Line(IEnumerable<Point<double>> data, Color color, string label)
		{
			d_data = new List<Point<double>>(data);
			d_label = label;
			d_lineWidth = 1;
			
			d_unprocessed = 0;
			
			d_xrange = new Range<double>(0, 0);
			d_yrange = new Range<double>(0, 0);
			
			d_sortedData = null;
			
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
		
		public virtual bool CanRule
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
			d_xrange.Freeze();
			d_yrange.Freeze();
			
			d_xrange.Update(0, 0);
			d_yrange.Update(0, 0);
			
			d_sortedData = null;
			bool makesorted = false;
			
			for (int i = 0; i < d_data.Count; ++i)
			{
				Point<double> p = d_data[i];

				if (i == 0 || p.X < d_xrange.Min)
				{
					d_xrange.Min = p.X;
				}
				
				if (i == 0 || p.X > d_xrange.Max)
				{
					d_xrange.Max = p.X;
				}
				
				if (i == 0 || p.Y < d_yrange.Min)
				{
					d_yrange.Min = p.Y;
				}
				
				if (i == 0 || p.Y > d_yrange.Max)
				{
					d_yrange.Max = p.Y;
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
			
			d_xrange.Thaw();
			d_yrange.Thaw();
		}
		
		public Range<double> XRange
		{
			get
			{
				return d_xrange;
			}
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
		
		public Range<double> YRange
		{
			get
			{
				return d_yrange;
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
		
		public string Label
		{
			get
			{
				return d_label;
			}
			set
			{
				d_label = value;
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
		
		public virtual void Render(Cairo.Context context, Point<double> scale, int idx)
		{
			Render(context, scale, idx, d_data.Count - idx);
		}
		
		public virtual void Render(Cairo.Context context, Point<double> scale, int idx, int length)
		{
			bool first = true;
			
			context.SetSourceRGB(d_color.R, d_color.G, d_color.B);
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
		
		private class PointComparer : IComparer<Point<double>>
		{		
			public int Compare(Point<double> a, Point<double> b)
			{
				return a.X < b.X ? -1 : (a.X > b.X ? 1 : 0);
			}
		}
		
		public double[] Sample(double[] sites, int fidx)
		{
			bool extrapolated;
			
			return Sample(sites, fidx, out extrapolated);
		}
		
		public double[] Sample(double[] sites, int fidx, out bool extrapolated)
		{
			double[] ret = new double[sites.Length];
			
			extrapolated = false;
			
			if (Count == 0)
			{
				return ret;
			}
			
			IComparer<Point<double>> comparer = new PointComparer();
			List<Point<double>> data;
			
			if (d_sortedData != null)
			{
				data = d_sortedData;
			}			
			else
			{
				data = d_data;
			}
			
			for (int i = 0; i < sites.Length; ++i)
			{
				int idx;
				Point<double> sp = new Point<double>(sites[i], 0);
				
				idx = data.BinarySearch(fidx, data.Count - fidx, sp, comparer);
				
				if (idx < 0)
				{
					idx = ~idx;
					
					if (idx == 0 || idx == data.Count)
					{
						extrapolated = true;
					}
				}
				
				fidx = idx > 0 ? idx - 1 : 0;
				int sidx = idx < data.Count ? idx : data.Count - 1;
				
				if (fidx >= data.Count || sidx >= data.Count)
				{
					ret[i] = data[data.Count - 1].Y;
				}
				else
				{
					Point<double> ps = data[sidx];
					Point<double> pf = data[fidx];

					double factor = ps.X == pf.X ? 1 : (ps.X - sites[i]) / (ps.X - pf.X);
					ret[i] = pf.Y * factor + (ps.Y * (1 - factor));
				}
			}
			
			return ret;
		}
		
		public double[] Sample(double[] sites)
		{
			return Sample(sites, 0);
		}
	}
}

