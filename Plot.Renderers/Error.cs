using System;
using Biorob.Math;
using System.Collections.Generic;

namespace Plot.Renderers
{
	public class Error : Line
	{
		public LineStyle d_errorLineStyle;
		public MarkerStyle d_errorMarkerStyle;
		public Color d_errorColor;
		public Color d_errorFillColor;
		private List<double> d_errorAbove;
		private List<double> d_errorBelow;
		private double d_markerSize;

		public Error()
		{
			d_errorLineStyle = LineStyle.Inherit;
			d_errorMarkerStyle = MarkerStyle.Dash;
			d_markerSize = 5;
		}
		
		private void ReconnectColor(ref Color color, Color val)
		{
			if (color == val)
			{
				return;
			}

			if (color != null)
			{
				color.Changed -= HandleColorChanged;
			}
			
			color = val;
			
			if (color != null)
			{
				color.Changed += HandleColorChanged;
			}
			
			EmitChanged();
		}

		private void HandleColorChanged(object sender, EventArgs e)
		{
			EmitChanged();
		}
		
		public Color ErrorFillColor
		{
			get { return d_errorFillColor; }
			set
			{
				ReconnectColor(ref d_errorFillColor, value);
			}
		}
		
		public Color ErrorColor
		{
			get { return d_errorColor; }
			set
			{
				ReconnectColor(ref d_errorColor, value);
			}
		}
		
		public LineStyle ErrorLineStyle
		{
			get { return d_errorLineStyle; }
			set
			{
				if (d_errorLineStyle != value)
				{
					d_errorLineStyle = value;
					EmitChanged();
				}
			}
		}
		
		public double ErrorMarkerSize
		{
			get { return d_markerSize; }
			set
			{
				if (d_markerSize != value)
				{
					d_markerSize = value;
					EmitChanged();
				}
			}
		}	
		
		public MarkerStyle ErrorMarkerStyle
		{
			get { return d_errorMarkerStyle; }
			set
			{
				if (d_errorMarkerStyle != value)
				{
					d_errorMarkerStyle = value;
					EmitChanged();
				}
			}
		}
		
		public IEnumerable<double> ErrorAbove
		{
			get { return d_errorAbove; }
			set
			{
				if (value == null)
				{
					d_errorAbove = null;
				}
				else
				{
					d_errorAbove = new List<double>(value);
				}

				EmitChanged();
			}
		}
		
		public IEnumerable<double> ErrorBelow
		{
			get { return d_errorBelow; }
			set
			{
				if (value == null)
				{
					d_errorBelow = null;
				}
				else
				{
					d_errorBelow = new List<double>(value);
				}

				EmitChanged();
			}
		}
		
		private void CreatePath(Cairo.Context context, Point scale, List<double> vals, int direction, bool closeonaxis, bool reverse)
		{
			if (Count <= 1)
			{
				return;
			}
			
			if (closeonaxis)
			{
				Point first = this[reverse ? Count - 1 : 0];
				context.MoveTo(first.X * scale.X, 0);
			}
			
			int start = reverse ? Count - 1 : 0;
			int dir = reverse ? -1 : 1;

			while (start >= 0 && start < Count)
			{
				Point pt = this[start];
				context.LineTo(pt.X * scale.X, (pt.Y + vals[start] * direction) * scale.Y);
				
				start += dir;
			}
			
			if (closeonaxis)
			{
				Point last = this[reverse ? 0 : Count - 1];
				context.LineTo(last.X * scale.X, 0);
			}
		}
		
		private void RenderErrorArea(Cairo.Context context, Point scale)
		{
			bool above = (d_errorAbove != null && d_errorAbove.Count == Count);
			bool below = (d_errorBelow != null && d_errorBelow.Count == Count);
			
			if (above && below)
			{
				CreatePath(context, scale, d_errorAbove, 1, true, false);
				CreatePath(context, scale, d_errorBelow, -1, true, true);
			}
			else if (above)
			{
				CreatePath(context, scale, d_errorAbove, 1, true, false);
			}
			else if (below)
			{
				CreatePath(context, scale, d_errorBelow, -1, true, false);
			}
			
			context.Fill();
		}
		
		private void RenderErrorBars(Cairo.Context context, Point scale, List<double> errors, int direction)
		{
			if (errors == null || errors.Count != Count)
			{
				return;
			}
			
			context.Save();
			
			for (int i = 0; i < Count; ++i)
			{
				Point pt = this[i];

				context.MoveTo(pt.X * scale.X, pt.Y * scale.Y);
				context.RelLineTo(0, errors[i] * scale.Y * direction);
			}
			
			if (d_errorColor != null)
			{
				d_errorColor.Set(context);
			}
			else if (Color != null)
			{
				Color.Set(context);
			}
			
			context.Stroke();
			context.Restore();
		}
		
		private void RenderErrorBars(Cairo.Context context, Point scale)
		{
			LineStyle lt;
			
			if (d_errorLineStyle == LineStyle.Inherit)
			{
				lt = LineStyle;
			}
			else
			{
				lt = d_errorLineStyle;
			}
			
			if (lt == LineStyle.None || lt == LineStyle.Inherit)
			{
				return;
			}
			
			context.Save();
			
			SetLineStyle(context, LineWidth, lt);
			
			RenderErrorBars(context, scale, d_errorAbove, 1);
			RenderErrorBars(context, scale, d_errorBelow, -1);
			
			context.Restore();
		}
		
		private void RenderErrorMarks(Cairo.Context context, Point scale, MarkerStyle style, double size, List<double> vals, int direction)
		{
			if (vals == null || vals.Count != Count)
			{
				return;
			}
			
			context.Save();
			
			if (d_errorColor != null)
			{
				d_errorColor.Set(context);
			}
			else if (Color != null)
			{
				Color.Set(context);
			}
			
			context.LineWidth = LineWidth;
			
			Marker.Renderer renderer = Marker.Lookup(style);
			
			for (int i = 0; i < Count; ++i)
			{
				Point pt = this[i].Copy();
				pt.Y += direction * vals[i];
				
				renderer(context, scale, pt, size, LineWidth);
			}
			
			context.Restore();
		}
		
		private void RenderErrorMarks(Cairo.Context context, Point scale)
		{
			MarkerStyle lt;
			double size;
			
			if (d_errorMarkerStyle == MarkerStyle.Inherit)
			{
				lt = MarkerStyle;
				size = MarkerSize;
			}
			else
			{
				lt = d_errorMarkerStyle;
				size = d_markerSize;
			}
			
			if (lt == MarkerStyle.None || lt == MarkerStyle.Inherit)
			{
				return;
			}

			RenderErrorMarks(context, scale, lt, size, d_errorAbove, 1);
			RenderErrorMarks(context, scale, lt, size, d_errorBelow, -1);
		}
		
		private void RenderError(Cairo.Context context, Point scale)
		{
			if (d_errorFillColor != null)
			{
				context.Save();
				
				d_errorFillColor.Set(context);
				RenderErrorArea(context, scale);
				
				context.Restore();
			}
			
			RenderErrorBars(context, scale);
			RenderErrorMarks(context, scale);
		}
		
		public override void Render(Cairo.Context context, Point scale)
		{
			RenderError(context, scale);

			// Render the line itself
			base.Render(context, scale);
		}
	}
}

