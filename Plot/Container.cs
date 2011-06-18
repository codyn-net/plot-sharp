using System;
using System.Collections.Generic;

namespace Plot
{
	public class Container : IComparer<Point<double>>
	{
		private List<Point<double>> d_data;

		private Color d_color;
		private string d_label;
		private int d_unprocessed;
		private Range<double> d_xrange;
		private Range<double> d_yrange;
		
		public event EventHandler Changed = delegate {};
		
		public Container(IEnumerable<Point<double>> data, Color color, string label)
		{
			d_data = new List<Point<double>>(data);
			d_label = label;
			
			d_unprocessed = 0;
			
			d_xrange = new Range<double>(0, 0);
			d_yrange = new Range<double>(0, 0);
			
			UpdateRanges();
			
			Color = d_color;
		}

		private void HandleColorChanged(object sender, EventArgs e)
		{
			Changed(this, new EventArgs());
		}
		
		public Container(IEnumerable<Point<double>> data, Color color) : this(data, color, "")
		{
		}
		
		public Container(IEnumerable<Point<double>> data) : this(data, null, "")
		{
		}
		
		private void UpdateRanges()
		{
			bool first = true;
			
			d_xrange.Min = 0;
			d_yrange.Min = 0;
			d_xrange.Max = 0;
			d_yrange.Max = 0;

			foreach (Point<double> p in d_data)
			{
				if (first || p.X < d_xrange.Min)
				{
					d_xrange.Min = p.X;
				}
				
				if (first || p.X > d_xrange.Max)
				{
					d_xrange.Max = p.X;
				}
				
				if (first || p.Y < d_yrange.Min)
				{
					d_yrange.Min = p.Y;
				}
				
				if (first || p.Y > d_yrange.Max)
				{
					d_yrange.Max = p.Y;
				}
				
				first = false;
			}
		}
		
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
				
				Changed(this, new EventArgs());
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
		
		public string Label
		{
			get
			{
				return d_label;
			}
			set
			{
				d_label = value;
				Changed(this, new EventArgs());
			}
		}
		
		public void Append(Point<double> pt)
		{
			d_data.Add(pt);
			++d_unprocessed;
		}
		
		public void Processed()
		{
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
				
				if (idx >= d_data.Count)
				{
					return new Point<double>();
				}
				else
				{
					return d_data[idx];
				}
			}
		}
		
		public int Compare(Point<double> a, Point<double> b)
		{
			return a.X < b.X ? -1 : (a.X > b.X ? 1 : 0);
		}
		
		public double[] Sample(double[] sites, int fidx)
		{
			double[] ret = new double[sites.Length];
			
			if (d_data.Count == 0)
			{
				return ret;
			}
			
			for (int i = 0; i < sites.Length; ++i)
			{
				int idx = d_data.BinarySearch(fidx, d_data.Count - fidx, new Point<double>(sites[i], 0), this);
				
				if (idx < 0)
				{
					idx = ~idx;
				}
				
				fidx = idx > 0 ? idx - 1 : 0;
				int sidx = idx < d_data.Count ? idx : d_data.Count - 1;
				
				if (fidx >= d_data.Count || sidx >= d_data.Count)
				{
					ret[i] = d_data[d_data.Count - 1].Y;
				}
				else
				{
					Point<double> ps = d_data[sidx];
					Point<double> pf = d_data[fidx];

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

