using System;
using System.Collections.Generic;

namespace Plot
{
	public class LineSeries
	{
		private List<Point<double>> d_data;

		private Color d_color;
		private string d_label;
		private int d_unprocessed;
		private int d_lineWidth;
		private Range<double> d_xrange;
		private Range<double> d_yrange;
		
		public event EventHandler Changed = delegate {};
		
		public LineSeries(IEnumerable<Point<double>> data, Color color, string label)
		{
			d_data = new List<Point<double>>(data);
			d_label = label;
			d_lineWidth = 1;
			
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
		
		public LineSeries(IEnumerable<Point<double>> data, Color color) : this(data, color, "")
		{
		}
		
		public LineSeries(IEnumerable<Point<double>> data) : this(data, null, "")
		{
		}
		
		public LineSeries(Color color, string name) : this(new Point<double>[] {}, color, "")
		{
		}
		
		public LineSeries(string name) : this(null, "")
		{
		}
		
		public LineSeries() : this("")
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
		
		public int LineWidth
		{
			get
			{
				return d_lineWidth;
			}
			set
			{
				d_lineWidth = value;
				Changed(this, new EventArgs());
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
	}
}

