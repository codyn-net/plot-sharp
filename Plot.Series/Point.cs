using System;
using System.Collections.Generic;

namespace Plot.Series
{
	public class Point : Line
	{
		public enum PointStyle
		{
			None,
			Circle,
			FilledCircle,
			Square,
			FilledSquare,
			Cross
		}

		private double d_size;
		private PointStyle d_style;
		private bool d_showLines;
		
		private delegate void RenderFunc(Cairo.Context context, Point<double> scale, Point<double> item, int idx);
		private RenderFunc d_renderer;	
		
		public Point(IEnumerable<Point<double>> data, Color color, string label) : base(data, color, label)
		{
			d_size = 1;
			
			Style = PointStyle.FilledCircle;
		}

		public Point(IEnumerable<Point<double>> data, Color color) : this(data, color, "")
		{
		}
		
		public Point(IEnumerable<Point<double>> data) : this(data, null, "")
		{
		}

		public Point(Color color, string name) : this(new Point<double>[] {}, color, name)
		{
		}
		
		public Point(string name) : this(null, name)
		{
		}
		
		public Point() : this("")
		{
		}
		
		public override bool CanRule
		{
			get
			{
				return d_showLines && base.CanRule;
			}
		}
		
		public bool ShowLines
		{
			get
			{
				return d_showLines;
			}
			set
			{
				if (d_showLines != value)
				{
					d_showLines = value;
					EmitChanged();
				}
			}
		}
		
		public double Size
		{
			get
			{
				return d_size;
			}
			set
			{
				if (d_size != value)
				{
					d_size = value;
				}

				EmitChanged();
			}
		}
		
		public PointStyle Style
		{
			get
			{
				return d_style;
			}
			set
			{
				d_style = value;
				
				switch (d_style)
				{
					case PointStyle.Circle:
						d_renderer = RenderCircle;
					break;
					case PointStyle.FilledCircle:
						d_renderer = RenderFilledCircle;
					break;
					case PointStyle.FilledSquare:
						d_renderer = RenderFilledSquare;
					break;
					case PointStyle.Square:
						d_renderer = RenderSquare;
					break;
					case PointStyle.Cross:
						d_renderer = RenderCross;
					break;
					case PointStyle.None:
						d_renderer = null;
					break;
				}

				EmitChanged();
			}
		}
		
		private void RenderCircle(Cairo.Context context, Point<double> scale, Point<double> item, int idx)
		{
			context.Arc(item.X * scale.X, item.Y * scale.Y, d_size / 2, 0, 2 * Math.PI);
			context.Stroke();
		}
		
		private void RenderFilledCircle(Cairo.Context context, Point<double> scale, Point<double> item, int idx)
		{
			context.Arc(item.X * scale.X, item.Y * scale.Y, d_size / 2, 0, 2 * Math.PI);
			context.Fill();
		}
		
		private void MakeSquare(Cairo.Context context, Point<double> scale, Point<double> item, int idx)
		{
			context.MoveTo(item.X * scale.X, item.Y * scale.Y);
			context.RelMoveTo(-d_size / 2, -d_size /  2);
			context.RelLineTo(d_size, 0);
			context.RelLineTo(0, d_size);
			context.RelLineTo(-d_size, 0);
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
			context.Fill();
		}
		
		private void RenderCross(Cairo.Context context, Point<double> scale, Point<double> item, int idx)
		{
			context.MoveTo(item.X * scale.X, item.Y * scale.Y);
			context.RelMoveTo(-d_size / 2, -d_size /  2);
			context.RelLineTo(d_size, d_size);
			context.Stroke();
			
			context.MoveTo(item.X * scale.X, item.Y * scale.Y);
			context.RelMoveTo(d_size / 2, -d_size /  2);
			context.RelLineTo(-d_size, d_size);
			context.Stroke();
		}
		
		public override void Render(Cairo.Context context, Point<double> scale, int idx, int length)
		{
			if (d_showLines)
			{
				context.Save();
				base.Render(context, scale, idx, length);
				context.Restore();
			}

			if (d_renderer == null)
			{
				return;
			}

			context.SetSourceRGB(Color.R, Color.G, Color.B);
			context.LineWidth = LineWidth;

			int curidx = idx;
			
			foreach (Point<double> item in Range(idx, length))
			{
				d_renderer(context, scale, item, curidx);
				curidx++;
			}
		}
	}
}

