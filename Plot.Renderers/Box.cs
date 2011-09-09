using System;
using Biorob.Math;

namespace Plot.Renderers
{
	public class Box : Renderer
	{
		private double d_borderWidth;

		private Color d_borderColor;
		private Color d_backgroundColor;
		
		private Point d_origin;
		
		private Alignment d_xalign;
		private Alignment d_yalign;
		
		private double d_borderRadius;

		public Box()
		{
			d_borderColor = null;
			d_backgroundColor = null;
			
			d_xalign = Alignment.Left;
			d_yalign = Alignment.Top;
			
			d_origin = new Point(0, 0);
			d_borderRadius = 0;
			
			XRange.Changed += HandleXRangeChanged;
			YRange.Changed += HandleYRangeChanged;
		}

		private void HandleYRangeChanged(object sender, EventArgs e)
		{
			RecalculateRanges();
		}

		private void HandleXRangeChanged(object sender, EventArgs e)
		{
			RecalculateRanges();
		}
		
		private void ReassignColor(ref Color source, Color target)
		{
			if (source == target)
			{
				return;
			}

			if (source != null)
			{
				source.Changed -= ChainChanged;
			}
			
			source = target;
			
			if (source != null)
			{
				source.Changed += ChainChanged;
			}
		}
		
		public Alignment YAlign
		{
			get
			{
				return d_yalign;
			}
			set
			{
				if (d_yalign != value)
				{
					d_yalign = value;
					
					RecalculateRanges();
					EmitChanged();
				}
			}
		}
		
		public Alignment XAlign
		{
			get
			{
				return d_xalign;
			}
			set
			{
				if (d_xalign != value)
				{
					d_xalign = value;
					
					RecalculateRanges();
					EmitChanged();
				}
			}
		}
		
		public double BorderRadius
		{
			get
			{
				return d_borderRadius;
			}
			set
			{
				if (d_borderRadius != value)
				{
					d_borderRadius = value;
					EmitChanged();
				}
			}
		}
		
		public double BorderWidth
		{
			get
			{
				return d_borderWidth;
			}
			set
			{
				if (d_borderWidth != value)
				{
					d_borderWidth = value;
					EmitChanged();
				}
			}
		}
		
		public Color BorderColor
		{
			get
			{
				return d_borderColor;
			}
			set
			{
				ReassignColor(ref d_borderColor, value);
			}
		}
		
		public Color BackgroundColor
		{
			get
			{
				return d_backgroundColor;
			}
			set
			{
				ReassignColor(ref d_backgroundColor, value);
			}
		}
		
		private void ChainChanged(object source, EventArgs args)
		{
			EmitChanged();
		}
		
		private void RecalculateRanges()
		{
			switch (d_xalign)
			{
				case Alignment.Left:
					d_origin.X = XRange.Min;
				break;
				case Alignment.Center:
					d_origin.X = 1.5 * XRange.Min - 0.5 * XRange.Max;
				break;
				case Alignment.Right:
					d_origin.X = 2 * XRange.Min - XRange.Max;
				break;
			}
			
			switch (d_yalign)
			{
				case Alignment.Top:
					d_origin.Y = YRange.Min;
				break;
				case Alignment.Center:
					d_origin.Y = 1.5 * YRange.Min - 0.5 * YRange.Max;
				break;
				case Alignment.Bottom:
					d_origin.Y = 2 * YRange.Min - YRange.Max;
				break;
			}
		}

        private void RoundedRectangle(Cairo.Context graphics, double x, double y, double width, double height, double radius)
        {
                double x1 = x + width;
                double y1 = y + height;

                graphics.MoveTo(x, y + radius);

                graphics.CurveTo(x, y, x, y, x + radius, y);
                graphics.LineTo(x1 - radius, y);

                graphics.CurveTo(x1, y, x1, y, x1, y + radius);
                graphics.LineTo(x1, y1 - radius);
                
                graphics.CurveTo(x1, y1, x1, y1, x1 - radius, y1);
                graphics.LineTo(x + radius, y1);
                
                graphics.CurveTo(x, y1, x, y1, x, y1 - radius);
                graphics.ClosePath();
        }
		
		public override void Render(Cairo.Context context, Point scale)
		{
			if ((d_borderColor == null || d_borderWidth <= 0) && d_backgroundColor == null)
			{
				return;
			}

			double px = UnitToPixel(XUnits, d_origin.X, scale.X);
			double py = UnitToPixel(YUnits, d_origin.Y, scale.Y);
			
			double dx = UnitToPixel(XUnits, XRange.Max - XRange.Min, scale.X);
			double dy = UnitToPixel(YUnits, YRange.Max - YRange.Min, scale.Y);
			
			if (d_borderRadius > 0)
			{
				RoundedRectangle(context, px, py, dx, dy, d_borderRadius);
			}
			else
			{
				context.Rectangle(px, py, dx, dy);
			}
			
			if (d_borderColor != null && d_borderWidth > 0)
			{
				context.LineWidth = d_borderWidth;
				d_borderColor.Set(context);

				context.StrokePreserve();
			}
			
			if (d_backgroundColor != null)
			{
				d_backgroundColor.Set(context);
				context.Fill();
			}
			
			context.NewPath();
		}
	}
}

