using System;
using System.Collections.Generic;
using System.Linq;

namespace Plot
{
	public class Graph : IDisposable
	{		
		private bool d_showRuler;
		private bool d_showLabels;
		private bool d_showGrid;
		private bool d_showRangeLabels;

		private List<Series.Line> d_data;
		
		// Axis
		private Range<double> d_xaxis;
		private AxisMode d_xaxisMode;

		private Range<double> d_yaxis;
		private AxisMode d_yaxisMode;
		
		private Range<double> d_dataXRange;
		private Range<double> d_dataYRange;
		
		private double d_axisAspect;
		private bool d_keepAxisAspect;

		private Cairo.Surface[] d_backbuffer;
		private int d_currentBuffer;
		private Point<double> d_ruler;
		private List<Ticks> d_xticks;
		private List<Ticks> d_yticks;
		private bool d_recreate;
		private bool d_checkingAspect;
		
		// User configurable
		private int d_ruleWhich;

		private Rectangle<int> d_dimensions;
		
		// Appearance
		private Pango.FontDescription d_font;

		// Colors		
		private Color d_backgroundColor;

		private Color d_axisColor;
		private ColorFgBg d_axisLabelColors;

		private Color d_rulerColor;
		private ColorFgBg d_rulerLabelColors;
		
		private static Color[] s_colors;
		private static int s_colorIndex;
		private bool d_antialias;
		
		public event RequestSurfaceHandler RequestSurface = delegate {};
		public event EventHandler RequestRedraw = delegate {};

		static Graph()
		{
			s_colors = new Color[] {
				new Color("#729fcf"),
				new Color("#8ae234"),
				new Color("#ef2929"),
				new Color("#fce94f"),
				new Color("#ad7fa8"),
				new Color("#fcaf3e"),
				new Color("#888a85"),
				new Color("#e9b96e")
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
		
		public bool KeepAspect
		{
			get
			{
				return d_keepAxisAspect;
			}
			set
			{
				if (value != d_keepAxisAspect)
				{
					d_keepAxisAspect = value;
					CheckAspect();
				}
			}
		}
		
		public bool Antialias
		{
			get
			{
				return d_antialias;
			}
			set
			{
				if (d_antialias != value)
				{
					d_antialias = value;
					d_recreate = true;
					
					EmitRequestRedraw();
				}
			}
		}
		
		public bool ShowLabels
		{
			get
			{
				return d_showLabels;
			}
			set
			{
				if (d_showLabels != value)
				{
					d_showLabels = value;
					EmitRequestRedraw();
				}
			}
		}
		
		public bool ShowRangeLabels
		{
			get
			{
				return d_showRangeLabels;
			}
			set
			{
				if (d_showRangeLabels != value)
				{
					d_showRangeLabels = value;
					EmitRequestRedraw();
				}
			}
		}
		
		public Graph(Range<double> xaxis, Range<double> yaxis)
		{
			d_showRuler = true;
			d_data = new List<Series.Line>();
			
			d_xaxis = xaxis;
			d_yaxis = yaxis;
			
			d_dataXRange = new Range<double>(0, 0);
			d_dataYRange = new Range<double>(0, 0);
			
			d_xticks = new List<Ticks>();
			d_yticks = new List<Ticks>();
			
			d_showGrid = false;
			d_showLabels = true;

			d_antialias = true;
			
			AddXTicks(new Ticks());
			AddYTicks(new Ticks());
			
			d_xaxisMode = AxisMode.Auto;
			d_yaxisMode = AxisMode.Auto;
			
			d_axisAspect = 1;
			d_keepAxisAspect = false;
			
			d_xaxis.Changed += delegate {
				d_recreate = true;
				
				CheckAspect();
				
				UpdateXTicks();
				EmitRequestRedraw();
			};
			
			d_yaxis.Changed += delegate {
				d_recreate = true;
				
				CheckAspect();
				
				UpdateYTicks();
				EmitRequestRedraw();
			};

			d_dimensions = new Rectangle<int>();
			
			d_dimensions.Resized += delegate {
				d_recreate = true;

				RemoveBuffer(d_backbuffer, 0);
				RemoveBuffer(d_backbuffer, 1);
				
				UpdateXTicks();
				UpdateYTicks();

				EmitRequestRedraw();
			};
			
			d_dimensions.Moved += delegate {
				UpdateXTicks();
				UpdateYTicks();

				EmitRequestRedraw();
			};
			
			d_backbuffer = new Cairo.Surface[2] {null, null};
			d_recreate = true;

			d_currentBuffer = 0;
			d_ruleWhich = 0;

			d_backgroundColor = new Color(1, 1, 1);
			
			d_axisColor = new Color(0, 0, 0);
			d_axisLabelColors = new ColorFgBg();
			
			d_rulerColor = new Color(0.5, 0.5, 0.5, 1);
			d_rulerLabelColors = new ColorFgBg();
			
			d_backgroundColor.Changed += RedrawWhenChanged;
			d_axisColor.Changed += RedrawWhenChanged;
			d_axisLabelColors.Changed += RedrawWhenChanged;
			d_rulerColor.Changed += RedrawWhenChanged;
			d_rulerLabelColors.Changed += RedrawWhenChanged;
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
				if (d_font != value)
				{
					d_font = value;

					EmitRequestRedraw();
				}
			}
		}
		
		public bool ShowGrid
		{
			get
			{
				return d_showGrid;
			}
			set
			{
				if (d_showGrid != value)
				{
					d_showGrid = value;
					EmitRequestRedraw();
				}
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
		
		public ColorFgBg AxisLabelColors
		{
			get
			{
				return d_axisLabelColors;
			}
		}
		
		public Color RulerColor
		{
			get
			{
				return d_rulerColor;
			}
		}
		
		public ColorFgBg RulerLabelColors
		{
			get
			{
				return d_rulerLabelColors;
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
		
		private void RedrawWhenChanged(object source, EventArgs args)
		{
			EmitRequestRedraw();
		}
		
		public Point<double> Ruler
		{
			get
			{
				return d_ruler;
			}
			set
			{
				if (d_ruler != value)
				{
					if (d_ruler != null)
					{
						d_ruler.Changed -= RedrawWhenChanged;
					}

					d_ruler = value;
					
					if (d_ruler != null)
					{
						d_ruler.Changed += RedrawWhenChanged;
					}

					EmitRequestRedraw();
				}
			}
		}
		
		public int Count
		{
			get
			{
				return d_data.Count;
			}
		}
		
		public Series.Line[] Series
		{
			get
			{
				return d_data.ToArray();
			}
		}
		
		public Range<double> DataXRange
		{
			get
			{
				return d_dataXRange;
			}
		}
		
		public Range<double> DataYRange
		{
			get
			{
				return d_dataYRange;
			}
		}
		
		public Range<double> YAxis
		{
			get
			{
				return d_yaxis;
			}
		}
		
		public AxisMode YAxisMode
		{
			get
			{
				return d_yaxisMode;
			}
		}
		
		public Range<double> XAxis
		{
			get
			{
				return d_xaxis;
			}
		}
		
		public AxisMode XAxisMode
		{
			get
			{
				return d_xaxisMode;
			}
		}
		
		public double AxisAspect
		{
			get
			{
				return d_axisAspect;
			}
			set
			{
				if (d_axisAspect != value)
				{
					d_axisAspect = value;
					CheckAspect();
				}
			}
		}
		
		private void OnXTicksChanged(object source, EventArgs args)
		{
			((Ticks)source).Update(d_xaxis, d_dimensions.Width);
			EmitRequestRedraw();
		}
		
		private void OnYTicksChanged(object source, EventArgs args)
		{
			((Ticks)source).Update(d_yaxis, d_dimensions.Height);
			EmitRequestRedraw();
		}
		
		public IEnumerable<Ticks> XTicks
		{
			get
			{
				return d_xticks;
			}
		}
		
		public IEnumerable<Ticks> YTicks
		{
			get
			{
				return d_yticks;
			}
		}
		
		public void AddXTicks(Ticks ticks)
		{
			d_xticks.Add(ticks);
			
			ticks.Changed += OnXTicksChanged;
		}
		
		public void RemoveXTicks(Ticks ticks)
		{
			ticks.Changed -= OnXTicksChanged;
			d_xticks.Remove(ticks);
		}
		
		public void AddYTicks(Ticks ticks)
		{
			d_yticks.Add(ticks);
			
			ticks.Changed += OnYTicksChanged;
		}
		
		public void RemoveYTicks(Ticks ticks)
		{
			ticks.Changed -= OnYTicksChanged;
			d_yticks.Remove(ticks);
		}

		public void Add(Series.Line series)
		{
			if (series.Color == null)
			{
				series.Color = NextColor();
			}

			d_data.Add(series);
			
			if (d_ruleWhich < 0)
			{
				d_ruleWhich = 0;
			}
			
			series.Changed += HandleLineSeriesChanged;
			series.XRange.Changed += HandleSeriesXRangeChanged;
			series.YRange.Changed += HandleSeriesYRangeChanged;
			
			HandleSeriesXRangeChanged(series.XRange, new EventArgs());
			HandleSeriesYRangeChanged(series.YRange, new EventArgs());
			
			Redraw();
		}
		
		private void UpdateXTicks()
		{
			foreach (Ticks ticks in d_xticks)
			{
				ticks.Update(d_xaxis, d_dimensions.Width);
			}
		}
		
		private void UpdateYTicks()
		{
			foreach (Ticks ticks in d_yticks)
			{
				ticks.Update(d_yaxis, d_dimensions.Height);
			}
		}
		
		private void CheckAspect()
		{
			if (!d_keepAxisAspect || d_checkingAspect)
			{
				return;
			}
			
			d_checkingAspect = true;
			
			// Update the viewport (xaxis, yaxis) according to the aspect ratio
			double aspect = d_yaxis.Span() / d_xaxis.Span();
			
			if (aspect < d_axisAspect)
			{
				// Yaxis should increase
				d_yaxis.Expand(d_xaxis.Span() * d_axisAspect - d_yaxis.Span());
			}
			else if (aspect > d_axisAspect)
			{
				// XAxis should increase
				d_xaxis.Expand(d_yaxis.Span() / d_axisAspect - d_xaxis.Span());
			}
			
			d_checkingAspect = false;
		}
		
		private delegate Range<double> SelectRange(Series.Line series);

		private void UpdateAxis(Range<double> range, AxisMode mode, Range<double> mine, Range<double> datarange, SelectRange selector)
		{
			mine.Freeze();
			datarange.Freeze();

			if (mode == AxisMode.AutoGrow)
			{
				// Simply only grow the ranges
				UpdateRange(range, mine);
			}
			else
			{
				Range<double> maxit = new Range<double>();
				bool first = true;

				foreach (Series.Line series in d_data)
				{
					Range<double> r = selector(series);
					
					if (first || r.Max > maxit.Max)
					{
						maxit.Max = r.Max;
					}
					
					if (first || r.Min < maxit.Min)
					{
						maxit.Min = r.Min;
					}
					
					first = false;
				}
				
				mine.Update(maxit);
			}
			
			CheckAspect();
			
			UpdateRange(range, datarange);

			mine.Thaw();
			datarange.Thaw();
		}
		
		private void UpdateRange(Range<double> range, Range<double> mine)
		{
			if (range.Max > mine.Max)
			{
				mine.Max = range.Max;
			}
			
			if (range.Min < mine.Min)
			{
				mine.Min = range.Min;
			}
		}

		private void HandleSeriesXRangeChanged(object sender, EventArgs e)
		{
			Range<double> r = (Range<double>)sender;
			
			if (d_xaxisMode != AxisMode.Fixed)
			{
				UpdateAxis(r, d_xaxisMode, d_xaxis, d_dataXRange, a => a.XRange);
			}
			else
			{
				UpdateRange(r, d_dataXRange);
			}
		}
		
		private void HandleSeriesYRangeChanged(object sender, EventArgs e)
		{
			Range<double> r = (Range<double>)sender;
			
			if (d_yaxisMode != AxisMode.Fixed)
			{
				UpdateAxis(r, d_yaxisMode, d_yaxis, d_dataYRange, a => a.YRange);
			}
			else
			{
				UpdateRange(r, d_dataYRange);
			}
		}

		private void HandleLineSeriesChanged(object sender, EventArgs e)
		{
			Series.Line series = (Series.Line)sender;

			HandleSeriesXRangeChanged(series.XRange, new EventArgs());
			HandleSeriesYRangeChanged(series.YRange, new EventArgs());

			Redraw();
		}
		
		public void Remove(Series.Line series)
		{
			if (!d_data.Contains(series))
			{
				return;
			}

			series.Changed -= HandleLineSeriesChanged;
			series.XRange.Changed -= HandleSeriesXRangeChanged;
			series.YRange.Changed -= HandleSeriesYRangeChanged;
			
			d_data.Remove(series);
			
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
				return new Point<double>(d_dimensions.Width / d_xaxis.Span(),
				                         d_dimensions.Height / d_yaxis.Span());
			}
		}

		public void ProcessAppend()
		{
			Series.Line maxunp = d_data.Aggregate(delegate (Series.Line a, Series.Line b) {
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
						
			foreach (Series.Line series in d_data)
			{
				Point<double> last = series[-1];
				int missing = m - series.Unprocessed;
				
				for (int i = maxunp.Count - missing; i < maxunp.Count; ++i)
				{
					series.Append(new Point<double>(maxunp[i].X, last.Y));
				}
			}
			
			RedrawUnprocessed(m);
			
			foreach (Series.Line series in d_data)
			{
				series.Processed();
			}
		}
		
		private Point<double> AxisTransform
		{
			get
			{
				Point<double> scale = Scale;
				
				return new Point<double>(-d_xaxis.Min * scale.X,
				                         -d_yaxis.Max * scale.Y);
			}
		}
		
		private void Prepare(Cairo.Context ctx)
		{
			Point<double> tr = AxisTransform;
			
			ctx.Scale(1, -1);
			ctx.Translate(RoundInBase(tr.X, 0.5), RoundInBase(tr.Y, 0.5));
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
		
		private void DrawAxisNumber(Cairo.Context ctx, double num, double x, double y, int margin, int padding, Alignment xalign, Alignment yalign, ColorFgBg colors, bool realignx, bool realigny)
		{
			DrawAxisNumber(ctx, num.ToString("0.000"), x, y, margin, padding, xalign, yalign, colors, realignx, realigny);	
		}
		
		private void DrawAxisNumber(Cairo.Context ctx, double num, double x, double y, int margin, int padding, Alignment xalign, Alignment yalign, ColorFgBg colors)
		{
			DrawAxisNumber(ctx, num.ToString("0.000"), x, y, margin, padding, xalign, yalign, colors, true, true);
		}
		
		private void DrawAxisNumber(Cairo.Context ctx, string num, double x, double y, int margin, int padding, Alignment xalign, Alignment yalign, ColorFgBg colors)
		{
			DrawAxisNumber(ctx, num, x, y, margin, padding, xalign, yalign, colors, true, true);	
		}
		
		private void DrawAxisNumber(Cairo.Context ctx, string num, double x, double y, int margin, int padding, Alignment xalign, Alignment yalign, ColorFgBg colors, bool realignx, bool realigny)
		{
			using (Pango.Layout layout = Pango.CairoHelper.CreateLayout(ctx))
			{
				layout.SetText(num);
				
				if (d_font != null)
				{
					layout.FontDescription = d_font.Copy();
					layout.FontDescription.Size = (int)(d_font.Size * 0.75);
				}
				
				int width, height;
				
				Pango.Rectangle inkrect;
				Pango.Rectangle logrect;
				
				layout.GetPixelExtents(out inkrect, out logrect);

				width = logrect.Width;
				height = logrect.Height;

				double x0;
				double y0;
				
				if (xalign == Alignment.Center)
				{
					x0 = x - width / 2 - padding;
				}
				else if (xalign == Alignment.Right)
				{
					x0 = x - margin - width - 2 * padding;
				}
				else
				{
					x0 = x + margin;
				}
				
				if (yalign == Alignment.Center)
				{
					y0 = y - height / 2 - padding;
				}
				else if (yalign == Alignment.Bottom)
				{
					y0 = y - margin - height;
				}
				else
				{
					y0 = y + margin;
				}
				
				if (realignx)
				{
					if (x0 < margin)
					{
						x0 = margin;
					}
					
					if (x0 + 2 * padding + width + margin > d_dimensions.Width)
					{
						x0 = d_dimensions.Width - margin - width - 2 * padding;
					}
				}
				
				if (realigny)
				{			
					if (y0 < margin)
					{
						y0 = margin;
					}
				
					if (y0 + 2 * padding + height + margin > d_dimensions.Height)
					{
						y0 = d_dimensions.Height - margin - height - 2 * padding;
					}
				}
				
				ctx.Save();
				
				ctx.Rectangle(x0, y0, width + 2 * padding, height + 2 * padding);
				colors.Bg.Set(ctx);
				ctx.Fill();
	
				colors.Fg.Set(ctx);
				ctx.MoveTo(x0 + padding, y0 + padding);
					
				Pango.CairoHelper.ShowLayout(ctx, layout);
				ctx.NewPath();
	
				ctx.Restore();
			}
		}
		
		private double RoundInBase(double r, double b)
		{
			if (r % 1 == 0)
			{
				return r - Math.Sign(r) * 0.5;
			}

			return Math.Round(r - b) + b;
		}
		
		private delegate void TicksRenderer(double coord, double v);
		
		public Point<double> AxisToPixel(Point<double> p)
		{
			Point<double> scale = Scale;
			Point<double> tr = AxisTransform;
			
			return new Point<double>(tr.X + p.X * scale.X, tr.Y + p.Y * scale.Y);
		}
		
		private void DrawTicks(Cairo.Context ctx, Ticks ticks, int i, TicksRenderer renderer)
		{
			Point<double> scale = Scale;
			Point<double> tr = AxisTransform;
			
			foreach (double p in ticks)
			{
				double o = RoundInBase(tr[i] + p * scale[i], 0.5);

				renderer(o, p);
			}
		}
		
		private void DrawXTicks(Cairo.Context ctx, Ticks ticks, double y, double ylbl)
		{
			y -= ticks.Length / 2;

			DrawTicks(ctx, ticks, 0, delegate (double x, double v) {
				if (ticks.Visible)
				{
					if (d_showGrid)
					{
						ctx.MoveTo(x, 0);
						ctx.RelLineTo(0, d_dimensions.Height);
					}
					else
					{
						ctx.MoveTo(x, y);
						ctx.RelLineTo(0, ticks.Length);
					}
	
					ctx.Stroke();
				}
				
				if (ticks.ShowLabels && v != 0)
				{
					DrawAxisNumber(ctx,
					               v.ToString(String.Format("F{0}", ticks.CalculatedTickDecimals)),
					               x,
					               ylbl,
					               2,
					               2,
					               Alignment.Center,
					               Alignment.Top,
					               d_axisLabelColors,
					               false,
					               true);
				}
			});
		}
		
		private void DrawYTicks(Cairo.Context ctx, Ticks ticks, double x, double lblx)
		{
			x -= ticks.Length / 2;

			DrawTicks(ctx, ticks, 1, delegate (double y, double v) {
				if (ticks.Visible)
				{
					if (d_showGrid)
					{
						ctx.MoveTo(0, -y);
						ctx.RelLineTo(d_dimensions.Width, 0);
					}
					else
					{
						ctx.MoveTo(x, -y);
						ctx.RelLineTo(ticks.Length, 0);
					}
					
					ctx.Stroke();
				}
				
				if (ticks.ShowLabels && v != 0)
				{
					DrawAxisNumber(ctx,
					               v.ToString(String.Format("F{0}", ticks.CalculatedTickDecimals)),
					               lblx,
					               -y,
					               2,
					               2,
					               Alignment.Right,
					               Alignment.Center,
					               d_axisLabelColors,
					               true,
					               false);
				}				
			});
		}

		private void DrawYAxis(Cairo.Context ctx)
		{
			d_axisColor.Set(ctx);
			ctx.LineWidth = 1;
			
			double axisx = 0.5;
			
			if (d_xaxis.Span() > 0)
			{
				axisx = RoundInBase(d_dimensions.Width / d_xaxis.Span() * -d_xaxis.Min, 0.5);
				
				ctx.MoveTo(axisx, 0);
				ctx.RelLineTo(0, d_dimensions.Height);
				ctx.Stroke();
			}
			
			double maxlen = 0;
			
			foreach (Ticks ticks in d_xticks)
			{
				if (ticks.Visible && ticks.Length > maxlen)
				{
					maxlen = ticks.Length;
				}
			}

			foreach (Ticks ticks in d_yticks)
			{
				DrawYTicks(ctx, ticks, axisx, axisx - maxlen / 2);
			}

			if (d_showRangeLabels)
			{
				DrawAxisNumber(ctx,
				               d_yaxis.Max,
				               axisx,
				               0,
				               2,
				               2,
				               Alignment.Right,
				               Alignment.Top,
				               d_axisLabelColors);

				DrawAxisNumber(ctx,
				               d_yaxis.Min,
				               axisx,
				               d_dimensions.Height,
				               2,
				               2,
				               Alignment.Right,
				               Alignment.Bottom,
				               d_axisLabelColors);
			}
		}

		private void DrawXAxis(Cairo.Context ctx)
		{
			d_axisColor.Set(ctx);
			ctx.LineWidth = 1;
			
			double axisy = 0.5;
			
			Point<double> tr = AxisTransform;

			if (d_yaxis.Span() > 0)
			{
				axisy = -RoundInBase(tr.Y, 0.5);
				
				ctx.MoveTo(0, axisy);
				ctx.RelLineTo(d_dimensions.Width, 0);
				ctx.Stroke();
			}
			
			double maxlen = 0;
			
			foreach (Ticks ticks in d_xticks)
			{
				if (ticks.Visible && ticks.Length > maxlen)
				{
					maxlen = ticks.Length;
				}
			}

			foreach (Ticks ticks in d_xticks)
			{
				DrawXTicks(ctx, ticks, axisy, axisy + maxlen / 2);
			}
			
			if (d_showRangeLabels)
			{
				DrawAxisNumber(ctx,
				               d_xaxis.Max,
				               d_dimensions.Width,
				               axisy,
				               2,
				               2,
				               Alignment.Right,
				               Alignment.Top,
				               d_axisLabelColors);

				DrawAxisNumber(ctx,
				               d_xaxis.Min,
				               0,
				               axisy,
				               2,
				               2,
				               Alignment.Left,
				               Alignment.Top,
				               d_axisLabelColors);
			}			
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
				
				foreach (Series.Line series in d_data)
				{
					series.Render(ctx, Scale, d_data[0].Count - num - 4);
				}
				
				ctx.Restore();
			}
			
			EmitRequestRedraw();
		}

		private void ClearBuffer(Cairo.Surface buf)
		{
			using (Cairo.Context ctx = new Cairo.Context(buf))
			{
				d_backgroundColor.Set(ctx);
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
			
			Cairo.Surface buf = SwapBuffer();
			
			if (buf == null)
			{
				return;
			}
			
			using (Cairo.Context ctx = new Cairo.Context(buf))
			{
				Prepare(ctx);
				
				SetAntialias(ctx);
				
				foreach (Series.Line series in d_data)
				{
					series.Render(ctx, Scale, 0);
				}
			}
		}
		
		public int RulerSeries
		{
			get
			{
				return d_ruleWhich;
			}
			set
			{
				if (d_ruleWhich != value && value <= d_data.Count && value >= 0)
				{
					d_ruleWhich = value;
					EmitRequestRedraw();
				}
			}
		}
		
		private void DrawRuler(Cairo.Context ctx)
		{
			if (d_ruler == null)
			{
				return;
			}

			d_rulerColor.Set(ctx);
			ctx.LineWidth = 1;
			
			double x = RoundInBase(d_ruler.X, 0.5);

			// Draw yline
			ctx.MoveTo(x, 0);
			ctx.LineTo(x, d_dimensions.Height);
			ctx.Stroke();

			Point<double> tr = AxisTransform;
			Point<double> pos = PixelToAxis(d_ruler);
			
			Series.Line series;
			double[] val = null;

			bool extrapolated;
			
			if (d_ruleWhich < 0 || d_ruleWhich >= d_data.Count || !d_data[d_ruleWhich].CanRule)
			{
				extrapolated = true;
			}
			else
			{
				series = d_data[d_ruleWhich];
				val = series.Sample(new double[] {pos.X}, 0, out extrapolated);
			}

			if (extrapolated)
			{
				double py = RoundInBase(d_ruler.Y, 0.5);
				
				// If there is not data to track, draw xline on pointer				
				ctx.MoveTo(0, py);
				ctx.RelLineTo(d_dimensions.Width, 0);
				ctx.Stroke();
				
				DrawAxisNumber(ctx,
				               pos.X,
				               x,
				               0,
				               2,
				               2,
				               Alignment.Left,
				               Alignment.Top,
				               d_rulerLabelColors);

				DrawAxisNumber(ctx,
				               pos.Y,
				               d_dimensions.Width,
				               py,
				               2,
				               2,
				               Alignment.Right,
				               Alignment.Top,
				               d_rulerLabelColors);

				return;
			}
			
			Point<double> scale = Scale;

			double yval = val[0];
			double y = RoundInBase(-tr.Y + yval * -scale.Y, 0.5);

			// Draw xline
			ctx.MoveTo(0, y);
			ctx.RelLineTo(d_dimensions.Width, 0);
			ctx.Stroke();
			
			// Draw label for x
			DrawAxisNumber(ctx,
			               pos.X,
			               d_dimensions.Width,
			               y,
			               2,
			               2,
			               Alignment.Right,
			               Alignment.Top,
			               d_rulerLabelColors);

			// Draw label for y
			DrawAxisNumber(ctx,
			               yval,
			               x,
			               0,
			               2,
			               2,
			               Alignment.Right,
			               Alignment.Top,
			               d_rulerLabelColors);
			
			// Draw circle
			ctx.LineWidth = 1;
			ctx.Arc(x, y, 4, 0, 2 * Math.PI);
			
			ctx.SetSourceRGBA(d_rulerColor.R, d_rulerColor.G, d_rulerColor.B, d_rulerColor.A * 0.5);
			ctx.FillPreserve();
			
			d_rulerColor.Set(ctx);
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
			if (d_data.Count == 0 || !d_showLabels)
			{
				return;
			}
			
			using (Pango.Layout layout = Pango.CairoHelper.CreateLayout(ctx))
			{			
				if (d_font != null)
				{
					layout.FontDescription = d_font;
				}
				
				List<string> labels = new List<string>();
				
				for (int i = 0; i < d_data.Count; ++i)
				{
					Series.Line series = d_data[i];

					if (!String.IsNullOrEmpty(series.Label))
					{
						string lbl = System.Security.SecurityElement.Escape(series.Label);
						string formatted = "<span color='" + HexColor(series.Color) + "'>" + lbl + "</span>";
						
						if (d_showRuler && d_ruleWhich == i)
						{
							formatted = "<b>" + formatted + "</b>";
						}

						labels.Add(formatted);
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
				
				ctx.Rectangle(2, 2, width + 4, height + 4);
				d_axisLabelColors.Bg.Set(ctx);
				ctx.Fill();
				
				ctx.MoveTo(4, 4);
				d_axisLabelColors.Fg.Set(ctx);
				
				Pango.CairoHelper.ShowLayout(ctx, layout);
			}
		}
		
		private void SetAntialias(Cairo.Context ctx)
		{
			if (d_antialias)
			{
				ctx.Antialias = Cairo.Antialias.Default;
			}
			else
			{
				ctx.Antialias = Cairo.Antialias.None;
			}
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
			
			SetAntialias(ctx);

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
			
			ctx.Save();
			DrawXAxis(ctx);
			ctx.Restore();
			
			if (d_showRuler)
			{
				DrawRuler(ctx);
			}
		}
		
		public void LookAt(Rectangle<double> rectangle)
		{
			d_xaxis.Freeze();
			d_yaxis.Freeze();
			
			XAxis.Update(rectangle.X, rectangle.X + rectangle.Width);
			YAxis.Update(rectangle.Y, rectangle.Y + rectangle.Height);
			
			d_xaxis.Thaw();
			d_yaxis.Thaw();
		}
		
		public void ZoomAt(Point<double> pt, double factorX, double factorY)
		{
			d_xaxis.Freeze();
			d_yaxis.Freeze();

			if (factorX > 0)
			{
				XAxis.Update(pt.X - (pt.X - d_xaxis.Min) / factorX,
				             pt.X + (d_xaxis.Max - pt.X) / factorX);
			}
			
			if (factorY > 0)
			{		
				YAxis.Update(pt.Y - (pt.Y - d_yaxis.Min) / factorY,
			    	         pt.Y + (d_yaxis.Max - pt.Y) / factorY);
			}
			
			d_xaxis.Thaw();
			d_yaxis.Thaw();
		}
		
		public void MoveBy(Point<double> pt)
		{
			d_xaxis.Freeze();
			d_yaxis.Freeze();
			
			XAxis.Update(d_xaxis.Min + pt.X, d_xaxis.Max + pt.X);
			YAxis.Update(d_yaxis.Min + pt.Y, d_yaxis.Max + pt.Y);
			
			d_xaxis.Thaw();
			d_yaxis.Thaw();
		}
		
		public Point<double> PixelToAxis(Point<double> pt)
		{
			return new Point<double>(d_xaxis.Min + pt.X * ((d_xaxis.Max - d_xaxis.Min) / d_dimensions.Width),
			                         d_yaxis.Min + (d_dimensions.Height - pt.Y) * ((d_yaxis.Max - d_yaxis.Min) / d_dimensions.Height));
		}
		
		public Point<double> ScaleFromPixel(Point<double> pt)
		{
			return new Point<double>(pt.X * ((d_xaxis.Max - d_xaxis.Min) / d_dimensions.Width),
			                         pt.Y * ((d_yaxis.Max - d_yaxis.Min) / d_dimensions.Height));
		}
		
		public void UpdateAxis(Range<double> xaxis, Range<double> yaxis)
		{
			d_xaxis.Freeze();
			d_yaxis.Freeze();

			d_xaxis.Update(xaxis);
			d_yaxis.Update(yaxis);
			
			d_xaxis.Thaw();
			d_yaxis.Thaw();
		}
	}
}
