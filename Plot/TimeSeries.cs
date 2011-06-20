using System;
using System.Collections.Generic;

namespace Plot
{
	public class TimeSeries : LineSeries, IComparer<Point<double>>
	{
		public TimeSeries(IEnumerable<Point<double>> data, Color color, string label) : base(data, color, label)
		{
		}

		public TimeSeries(IEnumerable<Point<double>> data, Color color) : this(data, color, "")
		{
		}
		
		public TimeSeries(IEnumerable<Point<double>> data) : this(data, null, "")
		{
		}

		public TimeSeries(Color color, string name) : this(new Point<double>[] {}, color, "")
		{
		}
		
		public TimeSeries(string name) : this(null, "")
		{
		}
		
		public TimeSeries() : this("")
		{
		}

		public int Compare(Point<double> a, Point<double> b)
		{
			return a.X < b.X ? -1 : (a.X > b.X ? 1 : 0);
		}
		
		public double[] Sample(double[] sites, int fidx)
		{
			double[] ret = new double[sites.Length];
			
			if (Count == 0)
			{
				return ret;
			}
			
			for (int i = 0; i < sites.Length; ++i)
			{
				int idx = PrivateData.BinarySearch(fidx, PrivateData.Count - fidx, new Point<double>(sites[i], 0), this);
				
				if (idx < 0)
				{
					idx = ~idx;
				}
				
				fidx = idx > 0 ? idx - 1 : 0;
				int sidx = idx < PrivateData.Count ? idx : PrivateData.Count - 1;
				
				if (fidx >= PrivateData.Count || sidx >= PrivateData.Count)
				{
					ret[i] = PrivateData[PrivateData.Count - 1].Y;
				}
				else
				{
					Point<double> ps = PrivateData[sidx];
					Point<double> pf = PrivateData[fidx];

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

