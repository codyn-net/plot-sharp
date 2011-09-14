using System;
using System.Collections.Generic;
using Biorob.Math;

namespace Plot.Renderers
{
	public class Area : Renderers.Line
	{
		private Color d_fillColor;

		public Area()
		{
			LineStyle = LineStyle.None;
		}
		
		public Color FillColor
		{
			get { return d_fillColor; }
			set
			{
				if (d_fillColor == value)
				{
					return;
				}

				if (d_fillColor != null)
				{
					d_fillColor.Changed -= HandleFillChanged;
				}
				
				d_fillColor = value;
				
				if (d_fillColor != null)
				{
					d_fillColor.Changed += HandleFillChanged;
				}
				
				EmitChanged();
			}
		}

		private void HandleFillChanged(object sender, EventArgs e)
		{
			EmitChanged();
		}
		
		private void RenderArea(Cairo.Context context, Point scale)
		{
			if (Count <= 1)
			{
				return;
			}
			
			Point first = this[0];
			Point last = this[Count - 1];
			
			context.MoveTo(first.X * scale.X, 0);

			foreach (Point pt in Data)
			{
				context.LineTo(pt.X * scale.X, pt.Y * scale.Y);
			}
			
			context.LineTo(last.X * scale.X, 0);
			context.ClosePath();
			
			if (d_fillColor != null)
			{
				d_fillColor.Set(context);
			}
			else if (Color != null)
			{
				Color.Set(context);
			}
			
			context.Fill();
		}

		public override void Render(Cairo.Context context, Point scale)
		{
			context.Save();
			RenderArea(context, scale);
			context.Restore();
			
			base.Render(context, scale);
		}
	}
}

