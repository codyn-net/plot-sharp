using System;
using System.Collections.Generic;
using System.Linq;

namespace Plot
{
	public class Graph : IDisposable
	{		
		private bool d_showRuler;
		private List<Container> d_data;
		
		private Range<double> d_xaxis;
		private Range<double> d_yaxis;

		private Cairo.Surface[] d_backbuffer;
		private int d_currentBuffer;
		private Point<int> d_ruler;
		private Ticks d_ticks;
		private bool d_recreate;

		// User configurable
		private int d_ruleWhich;
		private bool d_hasRuler;
		private Rectangle<int> d_dimensions;
		
		// Appearance
		private Pango.FontDescription d_font;

		// Colors		
		private Color d_backgroundColor;
		private Color d_axisColor;
		
		private static Color[] s_colors;
		private static int s_colorIndex;
		
		public event RequestSurfaceHandler RequestSurface = delegate {};
		public event EventHandler RequestRedraw = delegate {};

		static Graph()
		{
			s_colors = new Color[] {
				new Color(0, 0, 0.6),
				new Color(0, 0.6, 0),
				new Color(0.6, 0, 0),
				new Color(0, 0.6, 0.6),
				new Color(0.6, 0.6, 0),
				new Color(0.6, 0, 0.6),
				new Color(0.6, 0.6, 0.6),
				new Color(0, 0, 0)
			};
		}
		
		public static void ResetColors()
		{
			s_colorIndex = 0;
		}
		
		private static Color NextColor()
		{
			Color ret = s_colors[s_colorIndex];
			
			if (s_colorIndex + 1 == s_colors.Length)
			{
				s_colorIndex = 0;
			}
			else
			{
				++s_colorIndex;
			}
			
			return ret;
		}
		
		public Graph(Range<double> xaxis, Range<double> yaxis)
		{
			d_showRuler = true;
			d_data = new List<Container>();
			
			d_xaxis = xaxis;
			d_yaxis = yaxis;
			
			d_xaxis.Changed += delegate {
				d_recreate = true;
				EmitRequestRedraw();
			};
			
			d_yaxis.Changed += delegate {
				d_recreate = true;
				EmitRequestRedraw();
			};

			d_dimensions = new Rectangle<int>();
			
			d_dimensions.Resized += delegate {
				d_recreate = true;
				EmitRequestRedraw();
			};
			
			d_dimensions.Moved += delegate {
				EmitRequestRedraw();
			};
			
			d_backbuffer = new Cairo.Surface[2] {null, null};
			d_recreate = true;

			d_currentBuffer = 0;
			d_ruleWhich = 0;

			d_backgroundColor = new Color(1, 1, 1);
			d_axisColor = new Color(0, 0, 0);
			
			d_backgroundColor.Changed += delegate {
				Redraw();
			};
			
			d_axisColor.Changed += delegate {
				EmitRequestRedraw();
			};
		}
		
		public Graph() : this(new Range<double>(-1, 1), new Range<double>(1, -1))
		{
		}
		
		public void Dispose()
		{
			RemoveBuffer(d_backbuffer, 0);
			RemoveBuffer(d_backbuffer, 1);
		}
		
		public Pango.FontDescription Font
		{
			get
			{
				return d_font;
			}
			set
			{
				d_font = value;

				EmitRequestRedraw();
			}
		}
		
		public Color BackgroundColor
		{
			get
			{
				return d_backgroundColor;
			}
		}
		
		public Color AxisColor
		{
			get
			{
				return d_axisColor;
			}
		}
		
		public Rectangle<int> Dimensions
		{
			get
			{
				return d_dimensions;
			}
		}

		public bool ShowRuler
		{
			get
			{
				return d_showRuler;
			}
			set
			{
				d_showRuler = value;
				
			}
		}
		
		public void EmitRequestRedraw()
		{
			RequestRedraw(this, new EventArgs());
		}
		
		public Point<int> Ruler
		{
			get
			{
				return d_ruler;
			}
			set
			{
				d_ruler = value;
				d_hasRuler = true;

				EmitRequestRedraw();
			}
		}
		
		public bool HasRuler
		{
			get
			{
				return d_hasRuler;
			}
			set
			{
				d_hasRuler = value;
				EmitRequestRedraw();
			}
		}
		
		public int Count
		{
			get
			{
				return d_data.Count;
			}
		}
		
		public Container[] Plots
		{
			get
			{
				return d_data.ToArray();
			}
		}
		
		public Range<double> YAxis
		{
			get
			{
				return d_yaxis;
			}
		}
		
		public Range<double> XAxis
		{
			get
			{
				return d_xaxis;
			}
		}
		
		public void AutoAxis()
		{
			Range<double> range = new Range<double>(-3 , 3);
			
			bool isset = false;
			
			foreach (Container cont in d_data)
			{
				Point<double> min = cont.Data.Min();
				Point<double> max = cont.Data.Max();
				
				if (!isset || min.X < range.Min)
				{
					range.Min = min.X;
				}
				
				if (!isset || max.X > range.Max)
				{
					range.Max = max.X;
				}
				
				isset = true;
			}
			
			double dist = range.Span() / 2;
			d_yaxis.Update(range.Min - dist * 0.2, range.Max + dist * 0.2);
						
			Redraw();
		}
		
		public void SetTicks(double width, double start)
		{
			// width is the number of pixels per tick unit
			// start is the tick unit value from the left
			if (width == 0)
			{
				d_ticks = null;
			}
			else
			{
				d_ticks = new Ticks(start, width);
			}
		}
		
		public void SetTicks(int width)
		{
			SetTicks(width, 0);
		}

		public void Add(Container container)
		{
			if (container.Color == null)
			{
				container.Color = NextColor();
			}

			d_data.Add(container);
			
			if (d_ruleWhich < 0)
			{
				d_ruleWhich = 0;
			}
			
			container.Changed += HandleContainerChanged;
			
			Redraw();
		}

		private void HandleContainerChanged(object sender, EventArgs e)
		{
			Redraw();
		}
		
		public void Remove(Container container)
		{
			if (!d_data.Contains(container))
			{
				return;
			}

			container.Changed -= HandleContainerChanged;
			
			d_data.Remove(container);
			
			if (d_ruleWhich >= d_data.Count)
			{
				d_ruleWhich = d_data.Count - 1;
			}

			Redraw();
		}
		
		private Point<double> Scale
		{
			get
			{
				return new Point<double>((double)(d_dimensions.Width / d_xaxis.Span()),
				                         (double)(d_dimensions.Height / d_yaxis.Span()));
			}
		}

		public void ProcessAppend()
		{
			Container maxunp = d_data.Aggregate(delegate (Container a, Container b) {
				if (a.Unprocessed > b.Unprocessed)
				{
					return a;
				}
				else
				{
					return b;
				}
			});
			
			if (maxunp == null || maxunp.Unprocessed == 0)
			{
				return;
			}
			
			int m = maxunp.Unprocessed;
						
			foreach (Container container in d_data)
			{
				Point<double> last = container[-1];
				int missing = m - container.Unprocessed;
				
				for (int i = maxunp.Count - missing; i < maxunp.Count; ++i)
				{
					container.Append(new Point<double>(maxunp[i].X, last.Y));
				}
			}
			
			RedrawUnprocessed(m);
			
			foreach (Container container in d_data)
			{
				container.Processed();
			}
		}
		
		private void Prepare(Cairo.Context ctx)
		{
			Point<double> scale = Scale;
			
			double px = Math.Round(-d_xaxis.Min * scale.X);
			double py = Math.Round(d_yaxis.Max * scale.Y);
			
			ctx.Translate(px - 0.5, py - 0.5);
		}
		
		private void SetGraphLine(Cairo.Context ctx, Container container)
		{
			ctx.SetSourceRGB(container.Color.R, container.Color.G, container.Color.B);
			ctx.LineWidth = 2;
		}
		
		private void DrawTick(Cairo.Context ctx, double wh)
		{
			ctx.MoveTo(wh, 0);
			ctx.LineWidth = 1.5;
			ctx.LineTo(wh, -5.5);
			ctx.Stroke();
		}
		
		private void DrawSmallTick(Cairo.Context ctx, double wh)
		{
			ctx.LineWidth = 1;
			ctx.MoveTo(wh, 0);
			ctx.LineTo(wh, -3);
			ctx.Stroke();
		}
		
		private void DrawXAxis(Cairo.Context ctx, Range<double> xaxis)
		{
			Point<double> scale = Scale;
			
			ctx.SetSourceRGBA(d_axisColor.R, d_axisColor.G, d_axisColor.B, d_axisColor.A);
			ctx.LineWidth = 1;
			
			Console.WriteLine(scale.X);
			
			ctx.MoveTo(xaxis.Min * scale.X, 0);
			ctx.LineTo(xaxis.Max * scale.X, 0);

			ctx.Stroke();
			
			// Draw ticks
			if (d_ticks == null)
			{
				return;
			}
			
			// TODO: draw ticks
		}
		
		private double SampleWidth()
		{
			// The data width (considering xrange) corresponding to 1 pixel
			return 1 / Scale.X;
		}
		
		private void RedrawUnprocessed(int num)
		{
			if (d_backbuffer[d_currentBuffer] == null)
			{
				return;
			}
			
			Cairo.Surface prev = d_backbuffer[d_currentBuffer];
			Cairo.Surface buf = SwapBuffer();
			
			Range<double> xrange = new Range<double>(d_data[0][d_data[0].Count - num].X,
			                                         d_data[0][d_data[0].Count - 1].X);
			
			using (Cairo.Context ctx = new Cairo.Context(buf))
			{
				// Draw old backbuffer on it, shifted
				int offset = 0;
				Point<double> scale = Scale;

				if (xrange.Max > d_xaxis.Max)
				{
					double extra = xrange.Max - d_xaxis.Max;
					offset = (int)Math.Ceiling(extra * scale.X);
					
					d_xaxis.Shift(offset / scale.X);
				}
				
				double clipwidth = Math.Ceiling((d_data[0][d_data[0].Count - num - 2].X - d_xaxis.Min) * scale.X);
				
				ctx.Save();
				ctx.Rectangle(-0.5, -0.5, clipwidth, d_dimensions.Height + 1);
				ctx.Clip();

				ctx.SetSourceSurface(prev, -offset, 0);
				ctx.Paint();
				ctx.Restore();
				
				ctx.Save();
				ctx.Rectangle(clipwidth - 0.5, 0, d_dimensions.Width, d_dimensions.Height);
				ctx.Clip();
				
				xrange.Min = d_xaxis.Min + (clipwidth / scale.X) - SampleWidth() * 20;
				
				// draw the points we now need to draw, according to new shift
				Prepare(ctx);
				
				DrawXAxis(ctx, xrange);
				
				foreach (Container container in d_data)
				{
					Render(ctx, container, xrange, d_data[0].Count - num - 4);
				}
				
				ctx.Restore();
			}
			
			EmitRequestRedraw();
		}

		private void Render(Cairo.Context ctx, Container container, Range<double> xrange, int idx)
		{
			Point<double> scale = Scale;
			SetGraphLine(ctx, container);
			
			// Generate sites for samples in xrange
			List<double> sites = new List<double>();
			
			// Draw a point at every 2 pixels
			double sw = 4 * SampleWidth();
			double start = xrange.Min - (xrange.Min % sw);
			
			while (start <= xrange.Max + sw)
			{
				sites.Add(start);
				start += sw;
			}

			double[] data = container.Sample(sites.ToArray(), idx);
			
			for (int i = 0; i < data.Length; ++i)
			{
				if (i != 0)
				{
					Console.WriteLine(sites[i] + " :: " + (data[i] * scale.Y));
				}
				
				double px = Math.Floor(sites[i] * scale.X);
				double py = data[i] * scale.Y;

				if (i == 0)
				{
					ctx.MoveTo(px, -py);
				}
				else
				{
					ctx.LineTo(px, -py);
				}
			}

			ctx.Stroke();
		}
		
		private void ClearBuffer(Cairo.Surface buf)
		{
			using (Cairo.Context ctx = new Cairo.Context(buf))
			{
				ctx.SetSourceRGB(d_backgroundColor.R, d_backgroundColor.G, d_backgroundColor.B);
				ctx.Paint();
			}
		}
		
		private Cairo.Surface SwapBuffer()
		{
			int neg = d_currentBuffer == 0 ? 1 : 0;
			
			if (d_backbuffer[neg] == null)
			{
				d_backbuffer[neg] = CreateBuffer();
			}
			else
			{
				ClearBuffer(d_backbuffer[neg]);
			}
			
			d_currentBuffer = neg;
			return d_backbuffer[neg];
		}
		
		private Cairo.Surface CreateBuffer()
		{
			if (d_dimensions.Width == 0 || d_dimensions.Height == 0)
			{
				return null;
			}

			RequestSurfaceArgs args = new RequestSurfaceArgs(d_dimensions.Width, d_dimensions.Height);
			
			RequestSurface(this, args);
			
			Cairo.Surface surface = args.Surface;
			
			if (surface != null)
			{
				ClearBuffer(surface);
			}

			return surface;
		}
		
		private void Redraw()
		{
			d_recreate = true;
			EmitRequestRedraw();
		}
		
		public int Offset
		{
			get
			{
				return 0;
			}
		}
		
		private void RemoveBuffer(Cairo.Surface[] surfaces, int idx)
		{
			if (surfaces[idx] == null)
			{
				return;
			}
			
			surfaces[idx].Dispose();
			surfaces[idx] = null;
		}
		
		private void Recreate()
		{
			d_recreate = false;
			
			RemoveBuffer(d_backbuffer, 0);
			RemoveBuffer(d_backbuffer, 1);
			
			Cairo.Surface buf = SwapBuffer();
			
			if (buf == null)
			{
				return;
			}
			
			using (Cairo.Context ctx = new Cairo.Context(buf))
			{
				Prepare(ctx);
				DrawXAxis(ctx, d_xaxis);
				
				foreach (Container container in d_data)
				{
					Render(ctx, container, d_xaxis, 0);
				}
			}
		}
		
		private void DrawYAxis(Cairo.Context ctx)
		{
			double cx = d_dimensions.Width - 0.5;
			
			ctx.LineWidth = 1;
			ctx.SetSourceRGBA(d_axisColor.R, d_axisColor.G, d_axisColor.B, d_axisColor.A);
			//ctx.MoveTo(cx, 0);
			//ctx.LineTo(cx, Allocation.Height);
			//ctx.Stroke();
			
			string ym = (((int)(d_yaxis.Max * 100)) / 100.0).ToString();
			Cairo.TextExtents e = ctx.TextExtents(ym);
			ctx.MoveTo(cx - e.Width - 5, e.Height + 2);
			ctx.ShowText(ym);
			
			ym = (((int)(d_yaxis.Min * 100)) / 100.0).ToString();
			e = ctx.TextExtents(ym);
			ctx.MoveTo(cx - e.Width - 5, d_dimensions.Height - 2);
			ctx.ShowText(ym);
		}
		
		private void DrawRuler(Cairo.Context ctx)
		{
			if (d_ruler == null)
			{
				return;
			}

			ctx.SetSourceRGB(0.5, 0.6, 1);
			ctx.LineWidth = 1;

			ctx.MoveTo(d_ruler.X + 0.5, 0);
			ctx.LineTo(d_ruler.X + 0.5, d_dimensions.Height);
			ctx.Stroke();
			
			if (d_data.Count == 0)
			{
				return;
			}
			
			Container container = d_data[d_ruleWhich];

			Point<double> scale = Scale;
			Prepare(ctx);
			
			double pos = d_ruler.X / scale.X + d_xaxis.Min;
			double[] val = container.Sample(new double[] {pos});

			// First draw label
			string s = val[0].ToString("F3");
			Cairo.TextExtents e = ctx.TextExtents(s);
			
			Point<double> pt = new Point<double>(pos * scale.X - 0.5f, val[0] * -scale.Y);
			double top = d_yaxis.Max * -scale.Y;
			
			ctx.Rectangle(pt.X + 3, top + 1, e.Width + 4, e.Height + 4);
			ctx.SetSourceRGBA(1, 1, 1, 0.8);
			ctx.Fill();
			
			ctx.MoveTo(pt.X + 5, top + 3 + e.Height);
			ctx.SetSourceRGBA(0, 0, 0, 0.8);
			ctx.ShowText(s);
			ctx.Stroke();
			
			ctx.LineWidth = 1.5;
			ctx.Arc(pt.X, pt.Y, 4, 0, 2 * Math.PI);
			
			ctx.SetSourceRGBA(0.6, 0.6, 1, 0.5);
			ctx.FillPreserve();
			
			ctx.SetSourceRGB(0.5, 0.6, 1);
			ctx.Stroke();
		}
		
		private string HexColor(Color color)
		{
			return String.Format("#{0:x2}{1:x2}{2:x2}", 
			                     (int)(color.R * 255),
			                     (int)(color.G * 255),
			                     (int)(color.B * 255));
		}
		
		private void DrawLabel(Cairo.Context ctx)
		{
			if (d_data.Count == 0)
			{
				return;
			}
			
			Pango.Layout layout = Pango.CairoHelper.CreateLayout(ctx);
			
			if (d_font != null)
			{
				layout.FontDescription = d_font;
			}
			
			List<string> labels = new List<string>();
			
			foreach (Container container in d_data)
			{
				if (!String.IsNullOrEmpty(container.Label))
				{
					string lbl = System.Security.SecurityElement.Escape(container.Label);
					labels.Add("<span color='" + HexColor(container.Color) + "'>" + lbl + "</span>");
				}
			}
			
			string t = String.Join(", ", labels.ToArray());
			
			if (t == String.Empty)
			{
				return;
			}
			
			layout.SetMarkup(t);
			
			int width, height;
			layout.GetPixelSize(out width, out height);
			
			ctx.Rectangle(1, 1, width + 3, height + 3);
			ctx.SetSourceRGBA(1, 1, 1, 0.7);
			ctx.Fill();
			
			ctx.MoveTo(1, 1);
			ctx.SetSourceRGBA(d_axisColor.R, d_axisColor.G, d_axisColor.B, d_axisColor.A);
			
			Pango.CairoHelper.ShowLayout(ctx, layout);			
		}
		
		public void Draw(Cairo.Context ctx)
		{
			if (d_recreate)
			{
				Recreate();
			}
			
			if (d_backbuffer[d_currentBuffer] == null)
			{
				return;
			}
			
			ctx.Save();

			ctx.SetSourceSurface(d_backbuffer[d_currentBuffer], d_dimensions.X, d_dimensions.Y);
			ctx.Paint();
			
			ctx.Restore();
			
			// Paint label
			ctx.Save();
			DrawLabel(ctx);
			ctx.Restore();
			
			// Paint axis
			ctx.Save();
			DrawYAxis(ctx);
			ctx.Restore();
			
			if (d_showRuler && d_hasRuler)
			{
				DrawRuler(ctx);
			}
		}
	}
}
