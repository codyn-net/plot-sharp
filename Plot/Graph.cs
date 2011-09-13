using System;
using System.Collections.Generic;
using System.Linq;
using Biorob.Math;

namespace Plot
{
	public class Graph : IDisposable
	{		
		private bool d_showRuler;
		private bool d_showLabels;
		private bool d_showGrid;
		private bool d_showRangeLabels;
		private bool d_showBox;
		private bool d_showAxis;
		private bool d_snapRulerToData;
		private bool d_showRulerAxis;
		private bool d_autoRecolor;
		private bool d_rulerTracksData;
		private bool d_snapRulerToAxis;
		private int d_snapRulerToAxisFactor;
		private bool d_showSnapGrid;

		private Point d_autoMargin;

		private List<Renderers.Renderer> d_renderers;
		
		// Axis
		private Range d_xaxis;
		private AxisMode d_xaxisMode;

		private Range d_yaxis;
		private AxisMode d_yaxisMode;
		
		private Range d_renderersXRange;
		private Range d_renderersYRange;
		
		private double d_axisAspect;
		private bool d_keepAxisAspect;

		private Cairo.Surface[] d_backbuffer;
		private int d_currentBuffer;
		private Point d_ruler;
		private Ticks d_xticks;
		private Ticks d_yticks;
		private bool d_recreate;
		private bool d_checkingAspect;
		
		private Point d_previousAxisSpan;
		private Point d_previousAxisOrigin;
		
		private Dictionary<Renderers.ILabeled, List<Rectangle>> d_labelRegions;
		
		// User configurable

		private Rectangle d_dimensions;
		
		// Appearance
		private Pango.FontDescription d_font;

		// Colors		
		private Color d_backgroundColor;

		private Color d_axisColor;
		private ColorFgBg d_axisLabelColors;

		private Color d_rulerColor;
		private ColorFgBg d_rulerLabelColors;
		
		private Color d_gridColor;
		
		private ColorMap d_colorMap;
		private bool d_antialias;
		
		private Cairo.Surface d_overlayBuffer;
		private Cairo.Surface d_smoothBuffer;
		
		public event RequestSurfaceHandler RequestSurface = delegate {};
		public event EventHandler RequestRedraw = delegate {};
		
		public Graph(Range xaxis, Range yaxis)
		{
			d_renderers = new List<Renderers.Renderer>();
			
			d_xaxis = xaxis;
			d_yaxis = yaxis;
			
			d_renderersXRange = new Range(0, 0);
			d_renderersYRange = new Range(0, 0);
			
			d_xticks = new Ticks();
			d_xticks.Changed += OnXTicksChanged;

			d_yticks = new Ticks();
			d_yticks.Changed += OnYTicksChanged;
			
			d_backgroundColor = new Color();
			d_axisColor = new Color();
			d_rulerColor = new Color();
			d_gridColor = new Color();
			d_axisLabelColors = new ColorFgBg();
			d_rulerLabelColors = new ColorFgBg();
			
			d_autoMargin = new Point(0, 0.1);
			d_colorMap = ColorMap.Default.Copy();
			
			d_colorMap.Changed += delegate {
				UpdateColors();
			};
			
			d_autoMargin.Changed += delegate {
				RecalculateYAxis();
				RecalculateXAxis();
			};
			
			d_labelRegions = new Dictionary<Renderers.ILabeled, List<Rectangle>>();
			
			d_xaxis.Changed += delegate {
				CheckAspect();
				
				UpdateXTicks();
				Redraw();
				
				d_xaxisMode = AxisMode.Fixed;
			};
			
			d_yaxis.Changed += delegate {
				CheckAspect();
				
				UpdateYTicks();
				Redraw();
				
				d_yaxisMode = AxisMode.Fixed;
			};

			d_dimensions = new Rectangle();
			
			d_dimensions.Resized += delegate {
				d_recreate = true;

				RemoveBuffer(d_backbuffer, 0);
				RemoveBuffer(d_backbuffer, 1);
				RemoveBuffer(ref d_overlayBuffer);
				RemoveBuffer(ref d_smoothBuffer);
				
				UpdateXTicks();
				UpdateYTicks();
				
				CheckAspect();

				Redraw();
			};
			
			d_dimensions.Moved += delegate {
				UpdateXTicks();
				UpdateYTicks();

				EmitRequestRedraw();
			};
			
			d_backbuffer = new Cairo.Surface[2] {null, null};
			d_recreate = true;

			d_currentBuffer = 0;
			
			Settings.Default(this);
			
			d_backgroundColor.Changed += RedrawWhenChanged;
			d_axisColor.Changed += RedrawWhenChanged;
			d_axisLabelColors.Changed += RedrawWhenChanged;
			d_rulerColor.Changed += RedrawWhenChanged;
			d_rulerLabelColors.Changed += RedrawWhenChanged;
			d_gridColor.Changed += RedrawWhenChanged;
		}
		
		public Graph() : this(new Range(0, 1), new Range(-1, 1))
		{
		}
		
		public void Dispose()
		{
			RemoveBuffer(d_backbuffer, 0);
			RemoveBuffer(d_backbuffer, 1);
			RemoveBuffer(ref d_overlayBuffer);
			RemoveBuffer(ref d_smoothBuffer);
		}
		
		public ColorMap ColorMap
		{
			get
			{
				return d_colorMap;
			}
		}
		
		public bool ShowSnapGrid
		{
			get { return d_showSnapGrid; }
			set
			{
				if (d_showSnapGrid != value)
				{
					d_showSnapGrid = value;
					Redraw();
				}
			}
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
					
					if (!d_keepAxisAspect)
					{
						if (d_xaxisMode == AxisMode.Auto)
						{
							AxisModeChanged(ref d_xaxisMode, d_xaxis, d_renderersXRange, d_autoMargin.X);
						}
						
						if (d_yaxisMode == AxisMode.Auto)
						{
							AxisModeChanged(ref d_yaxisMode, d_yaxis, d_renderersYRange, d_autoMargin.Y);
						}
					}
				}
			}
		}
		
		public bool SnapRulerToAxis
		{
			get { return d_snapRulerToAxis; }
			set
			{
				if (d_snapRulerToAxis != value)
				{
					d_snapRulerToAxis = value;
					EmitRequestRedraw();
				}
			}
		}
		
		public int SnapRulerToAxisFactor
		{
			get { return d_snapRulerToAxisFactor; }
			set
			{
				if (d_snapRulerToAxisFactor != value)
				{
					d_snapRulerToAxisFactor = value;
					EmitRequestRedraw();
				}
			}
		}
		
		public bool RulerTracksData
		{
			get { return d_rulerTracksData; }
			set
			{
				if (d_rulerTracksData != value)
				{
					d_rulerTracksData = value;
					EmitRequestRedraw();
				}
			}
		}
		
		public bool AutoRecolor
		{
			get { return d_autoRecolor; }
			set
			{
				if (d_autoRecolor != value)
				{
					d_autoRecolor = value;
					
					UpdateColors();
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
					Redraw();
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
					Redraw();
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
					Redraw();
				}
			}
		}
		
		public bool ShowAxis
		{
			get
			{
				return d_showAxis;
			}
			set
			{
				if (d_showAxis != value)
				{
					d_showAxis = value;
					Redraw();
				}
			}
		}
		
		public Point AutoMargin
		{
			get
			{
				return d_autoMargin;
			}
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

					Redraw();
				}
			}
		}
		
		public bool ShowBox
		{
			get
			{
				return d_showBox;
			}
			set
			{
				if (d_showBox != value)
				{
					d_showBox = value;
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
					Redraw();
				}
			}
		}
		
		public Color GridColor
		{
			get
			{
				return d_gridColor;
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
		
		public Rectangle Dimensions
		{
			get
			{
				return d_dimensions;
			}
		}
		
		public bool ShowRulerAxis
		{
			get
			{
				return d_showRulerAxis;
			}
			set
			{
				if (d_showRulerAxis != value)
				{
					d_showRulerAxis = value;
					EmitRequestRedraw();
				}
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
				if (d_showRuler != value)
				{
					d_showRuler = value;
					EmitRequestRedraw();
				}
			}
		}
		
		public bool SnapRulerToData
		{
			get
			{
				return d_snapRulerToData;
			}
			set
			{
				if (d_snapRulerToData != value)
				{
					d_snapRulerToData = value;
					
					if (d_showRuler)
					{
						EmitRequestRedraw();
					}
				}
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
		
		public Point Ruler
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
				return d_renderers.Count;
			}
		}
		
		public Renderers.Renderer[] Renderers
		{
			get
			{
				return d_renderers.ToArray();
			}
		}
		
		public Range DataXRange
		{
			get
			{
				return d_renderersXRange;
			}
		}
		
		public Range DataYRange
		{
			get
			{
				return d_renderersYRange;
			}
		}
		
		public Range YAxis
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
			set
			{
				if (d_yaxisMode != value)
				{
					d_yaxisMode = value;

					AxisModeChanged(ref d_yaxisMode, d_yaxis, d_renderersYRange, d_autoMargin.Y);
				}
			}
		}
		
		public Range XAxis
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
			set
			{
				if (d_xaxisMode != value)
				{
					d_xaxisMode = value;
					
					AxisModeChanged(ref d_xaxisMode, d_xaxis, d_renderersXRange, d_autoMargin.X);
				}
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
			((Ticks)source).Update(d_xaxis, (int)d_dimensions.Width);
			Redraw();
		}
		
		private void OnYTicksChanged(object source, EventArgs args)
		{
			((Ticks)source).Update(d_yaxis, (int)d_dimensions.Height);
			Redraw();
		}
		
		public Ticks XTicks
		{
			get
			{
				return d_xticks;
			}
		}
		
		public Ticks YTicks
		{
			get
			{
				return d_yticks;
			}
		}
		
		private void UpdateColors()
		{
			if (!d_autoRecolor)
			{
				return;
			}

			int idx = 0;

			foreach (Renderers.Renderer renderer in d_renderers)
			{
				Renderers.IColored colored = renderer as Renderers.IColored;
				
				if (colored != null)
				{
					colored.Color = d_colorMap[idx++];
				}
			}
			
			Redraw();
		}

		public void Add(Renderers.Renderer renderer)
		{
			d_renderers.Add(renderer);
			
			UpdateColors();
			
			Renderers.IColored colored = renderer as Renderers.IColored;
			
			if (colored != null && colored.Color == null && !d_autoRecolor)
			{
				// Still assign a color
				colored.Color = d_colorMap[0];
			}
			
			if (d_renderers.Count == 1)
			{
				renderer.HasRuler = true;
			}
			
			renderer.Changed += HandleRendererChanged;
			renderer.RulerChanged += HandleRendererRulerChanged;

			renderer.XRange.Changed += HandleRendererXRangeChanged;
			renderer.YRange.Changed += HandleRendererYRangeChanged;
			
			HandleRendererXRangeChanged(renderer.XRange, new EventArgs());
			HandleRendererYRangeChanged(renderer.YRange, new EventArgs());
			
			Redraw();
		}
		
		private void HandleRendererRulerChanged(object source, EventArgs args)
		{
			Redraw();
		}
		
		private void UpdateXTicks()
		{
			d_xticks.Update(d_xaxis, (int)d_dimensions.Width);
		}
		
		private void UpdateYTicks()
		{
			d_yticks.Update(d_yaxis, (int)d_dimensions.Height);
		}
		
		private void CheckAspect()
		{
			if (!d_keepAxisAspect || d_checkingAspect)
			{
				return;
			}
			
			d_checkingAspect = true;
			
			// Update the viewport (xaxis, yaxis) according to the aspect ratio
			double aspect = (d_yaxis.Span / d_dimensions.Height) / (d_xaxis.Span / d_dimensions.Width);
			
			if (aspect < d_axisAspect)
			{
				// Yaxis should increase
				d_yaxis.Expand(-d_yaxis.Span + (d_xaxis.Span / d_dimensions.Width) * d_dimensions.Height * d_axisAspect);
			}
			else if (aspect > d_axisAspect)
			{
				// XAxis should increase
				d_xaxis.Expand(-d_xaxis.Span + (d_yaxis.Span / d_dimensions.Height) * d_dimensions.Width / d_axisAspect);
			}
			
			d_checkingAspect = false;
		}
		
		private delegate Range SelectRange(Renderers.Renderer renderer);
		
		private void UpdateDataRange(Range range, SelectRange selector)
		{
			range.Freeze();
			
			bool first = true;
			range.Update(0, 0);
			
			foreach (Renderers.Renderer renderer in d_renderers)
			{
				Range r = selector(renderer);

				if (first)
				{
					range.Update(r);
					first = false;
				}
				else
				{
					if (r.Max > range.Max)
					{
						range.Max = r.Max;
					}
					
					if (r.Min < range.Min)
					{
						range.Min = r.Min;
					}
				}
			}
			
			range.Thaw();
		}
		
		private void RecalculateAxis(ref AxisMode mode, Range axis, double margin, SelectRange selector)
		{
			if (mode == AxisMode.Fixed)
			{
				return;
			}

			axis.Freeze();
			bool first = true;
			
			foreach (Renderers.Renderer renderer in d_renderers)
			{
				Range r = selector(renderer);
				
				if (r == null)
				{
					continue;
				}

				if (first)
				{
					axis.Update(r);
					first = false;
				}
				else
				{
					if (r.Max > axis.Max)
					{
						axis.Max = r.Max;
					}
					
					if (r.Min < axis.Min)
					{
						axis.Min = r.Min;
					}
				}
			}
			
			if (margin == 0 && axis.Span <= Constants.Epsilon)
			{
				axis.Update(axis.Widen(1));
			}
			else
			{
				axis.Update(axis.Widen(margin));
			}

			CheckAspect();

			axis.Thaw();
			mode = AxisMode.Auto;
		}
		
		private void RecalculateXAxis()
		{
			RecalculateAxis(ref d_xaxisMode, d_xaxis, d_autoMargin.X, a => a.XRange);
		}
		
		private void RecalculateYAxis()
		{
			RecalculateAxis(ref d_yaxisMode, d_yaxis, d_autoMargin.Y, a => a.YRange);
		}

		private void HandleRendererXRangeChanged(object sender, EventArgs e)
		{
			RecalculateXAxis();
			UpdateDataRange(d_renderersXRange, a => a.XRange);
		}
		
		private void AxisModeChanged(ref AxisMode mode, Range range, Range dataRange, double margin)
		{
			if (mode == AxisMode.Auto)
			{
				if (d_renderers.Count > 0)
				{
					range.Update(dataRange.Widen(margin));
				}

				mode = AxisMode.Auto;
			}
		}
		
		private void HandleRendererYRangeChanged(object sender, EventArgs e)
		{
			RecalculateYAxis();			
			UpdateDataRange(d_renderersYRange, a => a.YRange);
		}

		private void HandleRendererChanged(object sender, EventArgs e)
		{
			Renderers.Renderer renderer = (Renderers.Renderer)sender;

			HandleRendererXRangeChanged(renderer.XRange, new EventArgs());
			HandleRendererYRangeChanged(renderer.YRange, new EventArgs());

			Redraw();
		}
		
		public void Remove(Renderers.Renderer renderer)
		{
			int idx = d_renderers.IndexOf(renderer);
			
			if (idx < 0)
			{
				return;
			}
			
			int prev;
			
			if (idx == 0)
			{
				prev = 0;
			}
			else
			{
				prev = idx - 1;
			}

			renderer.Changed -= HandleRendererChanged;
			renderer.RulerChanged -= HandleRendererRulerChanged;

			renderer.XRange.Changed -= HandleRendererXRangeChanged;
			renderer.YRange.Changed -= HandleRendererYRangeChanged;
			
			d_renderers.RemoveAt(idx);
			
			if (renderer.HasRuler)
			{
				renderer.HasRuler = false;

				if (d_renderers.Count > 0)
				{
					d_renderers[prev].HasRuler = true;
				}
			}
			
			UpdateColors();

			Redraw();
		}
		
		private Point Scale
		{
			get
			{
				return new Point(d_dimensions.Width / d_xaxis.Span,
				                 d_dimensions.Height / d_yaxis.Span);
			}
		}

		public void ProcessAppend()
		{
			/* TODO
			
			Renderers.Renderer maxunp = d_renderers.Aggregate(delegate (Renderers.Renderer a, Renderers.Renderer b) {
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
						
			foreach (Series.Line series in d_renderers)
			{
				Point last = series[-1];
				int missing = m - series.Unprocessed;
				
				for (int i = maxunp.Count - missing; i < maxunp.Count; ++i)
				{
					series.Append(new Point(maxunp[i].X, last.Y));
				}
			}
			
			RedrawUnprocessed(m);
			
			foreach (Series.Line series in d_renderers)
			{
				series.Processed();
			}*/
		}
		
		private Point AxisTransform
		{
			get
			{
				Point scale = Scale;
				
				return new Point(-d_xaxis.Min * scale.X,
				                 -d_yaxis.Max * scale.Y);
			}
		}
		
		private void Prepare(Cairo.Context ctx)
		{
			Point tr = AxisTransform;
			
			ctx.Scale(1, -1);
			ctx.Translate(tr.X, tr.Y);
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
		
		private double RoundInShift(double r, double b)
		{
			if (r % 1 == 0)
			{
				 return r - Math.Sign(r) * b;
			}

			return Math.Round(r - b) + b;
		}
		
		private double RoundInBase(double r, double b)
		{
			return Math.Round(r / b) * b;
		}
		
		private delegate void TicksRenderer(double coord, double v);
		
		public Point AxisToPixel(Point p)
		{
			Point scale = Scale;
			Point tr = AxisTransform;
			
			return new Point(tr.X + p.X * scale.X, -tr.Y - p.Y * scale.Y);
		}
		
		private void DrawTicks(Cairo.Context ctx, Ticks ticks, int i, TicksRenderer renderer)
		{
			Point scale = Scale;
			Point tr = AxisTransform;
			
			foreach (double p in ticks)
			{
				double o = RoundInShift(tr[i] + p * scale[i], 0.5);

				renderer(o, p);
			}
		}
		
		private void DrawXLabels(Cairo.Context ctx, Ticks ticks, double y, double ylbl)
		{
			if (d_showRangeLabels)
			{
				double axisy = 0.5;
				Point tr = AxisTransform;

				if (d_yaxis.Span > 0)
				{
					axisy = -RoundInShift(tr.Y, 0.5);
				}

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

			if (!ticks.ShowLabels)
			{
				return;
			}
			
			y -= ticks.Length / 2;
			
			DrawTicks(ctx, ticks, 0, delegate (double x, double v) {
				if (v == 0 || v > d_xaxis.Max || v < d_xaxis.Min)
				{
					return;
				}
				
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
			});
		}
		
		private void DrawSnappyGrid(Cairo.Context ctx, Ticks ticks, int idx, TicksRenderer renderer)
		{
			if (!d_showGrid || !d_showSnapGrid || !d_snapRulerToAxis || d_snapRulerToData || d_snapRulerToAxisFactor <= 1)
			{
				return;
			}

			Point scale = Scale;
			Point tr = AxisTransform;
			
			double dd = ticks.CalculatedTickSize / d_snapRulerToAxisFactor;
			
			foreach (double p in ticks)
			{
				for (int i = 1; i < d_snapRulerToAxisFactor; ++i)
				{
					double v = p + dd * i;
					double o = RoundInShift(tr[idx] + v * scale[idx], 0.5);
					renderer(o, v);
				}
			}
		}
		
		private void DrawXTicks(Cairo.Context ctx, Ticks ticks, double y, double ylbl)
		{
			if (!ticks.Visible)
			{
				return;
			}

			y -= ticks.Length / 2;
			
			DrawSnappyGrid(ctx, ticks, 0, delegate (double x, double v) {
				
				if (v > d_xaxis.Max || v < d_xaxis.Min || v == 0)
				{
					return;
				}
				
				d_gridColor.Set(ctx);
				ctx.MoveTo(x, 0);
				ctx.RelLineTo(0, d_dimensions.Height);
			});

			DrawTicks(ctx, ticks, 0, delegate (double x, double v) {
				if (v > d_xaxis.Max || v < d_xaxis.Min || v == 0)
				{
					return;
				}

				if (d_showGrid)
				{
					d_gridColor.Set(ctx);
					ctx.MoveTo(x, 0);
					ctx.RelLineTo(0, d_dimensions.Height);
				}
				else
				{
					d_axisColor.Set(ctx);

					ctx.MoveTo(x, y);
					ctx.RelLineTo(0, ticks.Length);
				}

				ctx.Stroke();
			});
		}
		
		private void DrawYLabels(Cairo.Context ctx, Ticks ticks, double x, double lblx)
		{
			if (d_showRangeLabels)
			{
				double axisx = 0.5;
				
				if (d_xaxis.Span > 0)
				{
					axisx = RoundInShift(d_dimensions.Width / d_xaxis.Span * -d_xaxis.Min, 0.5);
				}

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

			if (!ticks.ShowLabels)
			{
				return;
			}
			
			x -= ticks.Length / 2;

			DrawTicks(ctx, ticks, 1, delegate (double y, double v) {
				if (v == 0 || v > d_yaxis.Max || v < d_yaxis.Min)
				{
					return;
				}

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
			});
		}
		
		private void DrawYTicks(Cairo.Context ctx, Ticks ticks, double x, double lblx)
		{
			if (!ticks.Visible)
			{
				return;
			}

			x -= ticks.Length / 2;
			
			DrawSnappyGrid(ctx, ticks, 1, delegate (double y, double v) {
				if (v > d_yaxis.Max || v < d_yaxis.Min || v == 0)
				{
					return;
				}
				
				d_gridColor.Set(ctx);

				ctx.MoveTo(0, -y);
				ctx.RelLineTo(d_dimensions.Width, 0);
			});

			DrawTicks(ctx, ticks, 1, delegate (double y, double v) {
				if (v == 0 || v > d_yaxis.Max || v < d_yaxis.Min)
				{
					return;
				}

				if (d_showGrid)
				{
					d_gridColor.Set(ctx);

					ctx.MoveTo(0, -y);
					ctx.RelLineTo(d_dimensions.Width, 0);
				}
				else
				{
					d_axisColor.Set(ctx);

					ctx.MoveTo(x, -y);
					ctx.RelLineTo(ticks.Length, 0);
				}
				
				ctx.Stroke();
			});
		}
		
		private bool YAxisOnBorder(double xaxis)
		{
			return xaxis < 1 || xaxis > d_dimensions.Width - 1;
		}

		private void DrawYAxis(Cairo.Context ctx)
		{
			ctx.LineWidth = 1;
			
			double axisx = 0.5;
			
			if (d_xaxis.Span > 0)
			{
				axisx = RoundInShift(d_dimensions.Width / d_xaxis.Span * -d_xaxis.Min, 0.5);
			}
				
			DrawYTicks(ctx, d_yticks, axisx, axisx - d_yticks.Length / 2);
			
			if (d_showAxis && !YAxisOnBorder(axisx))
			{			
				d_axisColor.Set(ctx);
			
				ctx.MoveTo(axisx, 0);
				ctx.RelLineTo(0, d_dimensions.Height);
				ctx.Stroke();
			}
		}
		
		private bool XAxisOnBorder(double yaxis)
		{
			return yaxis < 1 || yaxis > d_dimensions.Height - 1;
		}

		private void DrawXAxis(Cairo.Context ctx)
		{
			ctx.LineWidth = 1;
			
			double axisy = 0.5;
			
			Point tr = AxisTransform;

			if (d_yaxis.Span > 0)
			{
				axisy = -RoundInShift(tr.Y, 0.5);
			}
			
			DrawXTicks(ctx, d_xticks, axisy, axisy + d_xticks.Length / 2);
			
			if (d_showAxis && !XAxisOnBorder(axisy))
			{			
				d_axisColor.Set(ctx);

				ctx.MoveTo(0, axisy);
				ctx.RelLineTo(d_dimensions.Width, 0);
				ctx.Stroke();
			}
		}
		
		private void YForXAxisLabel(out double axisy, out double axisylbl)
		{
			axisy = 0.5;
		
			Point tr = AxisTransform;
			
			if (d_yaxis.Span > 0)
			{
				axisy = -RoundInShift(tr.Y, 0.5);
			}
			
			axisylbl = axisy + d_yticks.Length / 2;
		}
		
		private void DrawAllXLabels(Cairo.Context ctx)
		{
			double axisy;
			double axisylbl;
			
			YForXAxisLabel(out axisy, out axisylbl);
			DrawXLabels(ctx, d_xticks, axisy, axisylbl);
		}
		
		private void XForYAxisLabel(out double axisx, out double axisxlbl)
		{
			axisx = 0.5;
			
			if (d_xaxis.Span > 0)
			{
				axisx = RoundInShift(d_dimensions.Width / d_xaxis.Span * -d_xaxis.Min, 0.5);
			}
			
			axisxlbl = axisx - d_xticks.Length / 2;
		}
		
		private void DrawAllYLabels(Cairo.Context ctx)
		{
			double axisx;
			double axisxlbl;
			
			XForYAxisLabel(out axisx, out axisxlbl);

			DrawYLabels(ctx, d_yticks, axisx, axisxlbl);
		}

		private void RedrawUnprocessed(int num)
		{
		}

		private void ClearBuffer(Cairo.Surface buf)
		{
			using (Cairo.Context ctx = new Cairo.Context(buf))
			{
				ctx.Operator = Cairo.Operator.Clear;
				ctx.Rectangle(0, 0, d_dimensions.Width, d_dimensions.Height);
				ctx.Fill();
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

			if (d_overlayBuffer == null)
			{
				d_overlayBuffer = CreateBuffer();
			}
			
			if (d_smoothBuffer == null)
			{
				d_smoothBuffer = CreateBuffer();
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

			RequestSurfaceArgs args = new RequestSurfaceArgs((int)d_dimensions.Width, (int)d_dimensions.Height);
			
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
		
		private void RemoveBuffer(ref Cairo.Surface surface)
		{
			if (surface != null)
			{
				surface.Destroy();
				surface = null;
			}
		}
		
		private void RemoveBuffer(Cairo.Surface[] surfaces, int idx)
		{
			if (surfaces[idx] == null)
			{
				return;
			}
			
			surfaces[idx].Destroy();
			surfaces[idx] = null;
		}
		
		private void DrawRenderers(Cairo.Context ctx)
		{
			Prepare(ctx);
				
			SetAntialias(ctx);
			
			foreach (Renderers.Renderer renderer in d_renderers)
			{
				ctx.Save();
				renderer.Render(ctx, Scale);
				ctx.Restore();
			}
		}
		
		private void ClipForPixel(Cairo.Context ctx, Cairo.Surface old)
		{
			// Can't do this if we don't have a previous buffer, or pixel dimensions
			if (old == null || d_previousAxisSpan == null)
			{
				return;
			}
			
			// Also only can do this when moving, not scaling
			if (Math.Abs(d_previousAxisSpan.X - d_xaxis.Span) > 1e-6 ||
			    Math.Abs(d_previousAxisSpan.Y - d_yaxis.Span) > 1e-6)
			{
				return;
			}
			
			Point s = Scale;
			
			// Determine the shift in pixels
			double shiftX = (d_previousAxisOrigin.X - d_xaxis.Min) * s.X;
			double shiftY = (d_previousAxisOrigin.Y - d_yaxis.Min) * s.Y;
			
			// We can only shift when we are shifting an integer number of pixels
			if (Math.Abs(Math.Round(shiftX) - shiftX) % 1 > 1e-6 || Math.Abs(Math.Round(shiftY) - shiftY) % 1 > 1e-6)
			{
				return;
			}
			
			// Blit the old surface into ctx, according to the shift in dimensions
			ctx.Save();
			
			int dx = (int)Math.Round(shiftX);
			int dy = (int)Math.Round(shiftY);
			
			old.Show(ctx,
			         d_dimensions.X + dx,
			         d_dimensions.Y - dy);
						
			ctx.Restore();
			
			// Then setup the clip region
			if (dx < 0)
			{
				// Shifted to the left, clip on the right
				ctx.Rectangle(d_dimensions.X + d_dimensions.Width + dx - 1,
				              d_dimensions.Y - 1,
				              -dx + 2,
				              d_dimensions.Height + 2);
			}
			else if (dx > 0)
			{
				// Shifted to the right, clip on the left
				ctx.Rectangle(d_dimensions.X - 1,
				              d_dimensions.Y - 1,
				              dx + 2,
				              d_dimensions.Height + 2);
			}
			
			if (dy < 0)
			{
				// Shifted to the top, clip the bottom
				ctx.Rectangle(d_dimensions.X - 1,
				              d_dimensions.Y - 1,
				              d_dimensions.Width + 2,
				              -dy + 2);
			}
			else if (dy > 0)
			{
				// Shifted to the bottom, clip the top
				ctx.Rectangle(d_dimensions.X - 1,
				              d_dimensions.Y + d_dimensions.Height - dy - 1,
				              d_dimensions.Width + 2,
				              dy + 2);
			}
			
			if (dx != 0 || dy != 0)
			{
				Cairo.Operator op = ctx.Operator;
				
				ctx.Operator = Cairo.Operator.Clear;
				ctx.ClipPreserve();
				ctx.Fill();
				
				ctx.Operator = op;
			}
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
				ctx.Rectangle(d_dimensions.X - 0.5, d_dimensions.Y - 0.5, d_dimensions.Width + 1, d_dimensions.Height + 1);
				ctx.Clip();

				// Setup clipping area according to the pixel dimensions
				//ClipForPixel(ctx, d_backbuffer[d_currentBuffer == 1 ? 0 : 1]);

				SetAntialias(ctx);

				// Then all the renderers
				ctx.Save();
				DrawRenderers(ctx);
				ctx.Restore();
			}
			
			if (HasOverlayBuffer)
			{
				using (Cairo.Context ctx = new Cairo.Context(d_overlayBuffer))
				{
					Cairo.Operator op = ctx.Operator;

					ctx.Operator = Cairo.Operator.Clear;
					ctx.Rectangle(0, 0, d_dimensions.Width, d_dimensions.Height);
					ctx.Fill();
					
					ctx.Operator = op;
					
					DrawOverlayBuffer(ctx);
				}
			}
			
			d_previousAxisSpan = new Point(d_xaxis.Span, d_yaxis.Span);
			d_previousAxisOrigin = new Point(d_xaxis.Min, d_yaxis.Min);
		}
		
		public Point SnapToAxis(Point inaxis, int factor)
		{
			double xb = d_xticks.CalculatedTickSize / factor;
			double yb = d_yticks.CalculatedTickSize / factor;
			
			return new Point(RoundInBase(inaxis.X, xb),
			                 RoundInBase(inaxis.Y, yb));
		}
		
		private void DrawRuler(Cairo.Context ctx)
		{
			if (d_ruler == null)
			{
				return;
			}

			d_rulerColor.Set(ctx);
			ctx.LineWidth = 1;
			
			Point tr = AxisTransform;
			Point pos = PixelToAxis(d_ruler);
			
			if (!d_rulerTracksData && !d_snapRulerToData && d_snapRulerToAxis)
			{
				pos = SnapToAxis(pos, d_snapRulerToAxisFactor);
			}
			
			Point pixels = AxisToPixel(pos);
			
			bool freestyle = true;
			bool first = true;
			
			Point scale = Scale;
			double x = RoundInShift(pixels.X, 0.5);
			
			if ((!d_snapRulerToData || !d_rulerTracksData) && d_showRulerAxis)
			{
				// Draw yline
				ctx.MoveTo(x, 0);
				ctx.LineTo(x, d_dimensions.Height);
				ctx.Stroke();
			}
			
			if (d_rulerTracksData)
			{
				foreach (Renderers.Renderer renderer in d_renderers)
				{
					if (!renderer.CanRule || !renderer.HasRuler)
					{
						continue;
					}
	
					bool interpolated;
					bool extrapolated;
	
					Point val;
					
					if (d_snapRulerToData)
					{
						interpolated = false;
						extrapolated = false;
	
						val = renderer.ValueClosestToX(pos.X);
						
						if (first)
						{
							first = false;
							 
							x = RoundInShift(tr.X + val.X * scale.X, 0.5);
		
							// Draw yline
							if (d_showRulerAxis)
							{
								ctx.MoveTo(x, 0);
								ctx.LineTo(x, d_dimensions.Height);
								ctx.Stroke();
							}
						}
					}
					else
					{
						val = renderer.ValueAtX(pos.X, out interpolated, out extrapolated);
					}
					
					if (extrapolated)
					{
						continue;
					}
					
					freestyle = false;
					
					double y = RoundInShift(-tr.Y + val.Y * -scale.Y, 0.5);
		
					// Draw xline
					Renderers.IColored colored = renderer as Renderers.IColored;
					ColorFgBg fgbg = new ColorFgBg();
		
					fgbg.Bg.Update(d_rulerLabelColors.Bg);
					fgbg.Fg.Update(d_rulerLabelColors.Fg);
					
					if (colored != null && colored.Color != null)
					{
						Color c = new Color(colored.Color);
						c.A *= 0.5;
	
						c.Set(ctx);
						fgbg.Fg.Update(colored.Color);
					}
					
					if (d_showRulerAxis)
					{	
						ctx.MoveTo(0, y);
						ctx.RelLineTo(d_dimensions.Width, 0);
						ctx.Stroke();
					}
	
					// Draw circle
					ctx.LineWidth = 1;
					ctx.Arc(tr.X + val.X * scale.X,
					        -tr.Y + val.Y * -scale.Y,
					        4, 0, 2 * Math.PI);
		
					d_backgroundColor.Set(ctx);
					ctx.FillPreserve();
					
					d_rulerColor.Set(ctx);
					ctx.Stroke();
	
					double axislbl;
					double dontcare;
					
					YForXAxisLabel(out dontcare, out axislbl);
					
					// Draw x value
					DrawAxisNumber(ctx,
					               val.X,
					               x,
					               axislbl,
					               2,
					               2,
					               Alignment.Center,
					               Alignment.Top,
					               d_rulerLabelColors);
		
					XForYAxisLabel(out dontcare, out axislbl);
		
					// Draw y value
					DrawAxisNumber(ctx,
					               val.Y,
					               axislbl,
					               y,
					               2,
					               2,
					               Alignment.Right,
					               Alignment.Center,
					               fgbg);
				}
			}
			
			if (freestyle)
			{
				if (d_snapRulerToData && d_rulerTracksData)
				{
					// Draw yline
					ctx.MoveTo(x, 0);
					ctx.LineTo(x, d_dimensions.Height);
					ctx.Stroke();
				}
				
				double py = RoundInShift(pixels.Y, 0.5);
				
				// If there is not data to track, draw xline on pointer			
				ctx.MoveTo(0, py);
				ctx.RelLineTo(d_dimensions.Width, 0);
				ctx.Stroke();
				
				double axislbl;
				double dontcare;
				
				YForXAxisLabel(out dontcare, out axislbl);
				
				DrawAxisNumber(ctx,
				               pos.X,
				               x,
				               axislbl,
				               2,
				               2,
				               Alignment.Center,
				               Alignment.Top,
				               d_rulerLabelColors);

				XForYAxisLabel(out dontcare, out axislbl);

				DrawAxisNumber(ctx,
				               pos.Y,
				               axislbl,
				               py,
				               2,
				               2,
				               Alignment.Right,
				               Alignment.Center,
				               d_rulerLabelColors);
			}

		}
		
		private string HexColor(Color color)
		{
			return String.Format("#{0:x2}{1:x2}{2:x2}", 
			                     (int)(color.R * 255),
			                     (int)(color.G * 255),
			                     (int)(color.B * 255));
		}
		
		private delegate string LabelSelector(Renderers.ILabeled labeled, out bool ismarkup);
		
		private void DrawLabels(Cairo.Context ctx)
		{
			d_labelRegions.Clear();

			if (!d_showLabels)
			{
				return;
			}
			
			DrawLabels(ctx, true, true, delegate (Renderers.ILabeled labeled, out bool ismarkup) {
				ismarkup = (labeled.YLabelMarkup != null);
				
				return ismarkup ? labeled.YLabelMarkup : labeled.YLabel;
			});
			
			DrawLabels(ctx, false, false, delegate (Renderers.ILabeled labeled, out bool ismarkup) {
				ismarkup = (labeled.XLabelMarkup != null);
				
				return ismarkup ? labeled.XLabelMarkup : labeled.XLabel;
			});
		}
		
		private void DrawLabels(Cairo.Context ctx, bool aligntop, bool alignleft, LabelSelector selector)
		{			
			using (Pango.Layout layout = Pango.CairoHelper.CreateLayout(ctx))
			{
				if (d_font != null)
				{
					layout.FontDescription = d_font;
				}
				
				Rectangle lastRegion = null;

				foreach (Renderers.Renderer renderer in d_renderers)
				{
					Renderers.ILabeled labeled = renderer as Renderers.ILabeled;
					
					if (labeled == null)
					{
						continue;
					}
					
					bool ismarkup;
					string lbl = selector(labeled, out ismarkup);
					
					if (lbl == null)
					{
						continue;
					}
					
					if (!ismarkup)
					{
						lbl = System.Security.SecurityElement.Escape(lbl);
					}

					Renderers.IColored colored = labeled as Renderers.IColored;

					string formatted;
					
					if (colored != null)
					{
						formatted = "<span color='" + HexColor(colored.Color) + "'>" + lbl + "</span>";
					}
					else
					{
						formatted = lbl;
					}
					
					if (d_showRuler && renderer.HasRuler)
					{
						formatted = "<b>" + formatted + "</b>";
					}
					
					layout.SetMarkup(formatted);
					
					int width, height;
					layout.GetPixelSize(out width, out height);
					
					Rectangle region = new Rectangle();

					if (lastRegion != null)
					{
						if (alignleft)
						{
							region.X = lastRegion.X + lastRegion.Width;
						}
						else
						{
							region.X = lastRegion.X - lastRegion.Width - width - 2;
						}

						region.Y = lastRegion.Y;
					}
					else
					{
						if (aligntop)
						{
							region.Y = 0;
						}
						else
						{
							region.Y = d_dimensions.Height - height;
						}
						
						if (alignleft)
						{
							region.X = 0;
						}
						else
						{
							region.X = d_dimensions.Width - width - 2;
						}
					}
					
					region.Width = width + 4;
					region.Height = height + 4;
					
					ctx.Rectangle(region.X, region.Y, region.Width, region.Height);
					d_axisLabelColors.Bg.Set(ctx);
					ctx.Fill();
					
					int xdir = alignleft ? 1 : -1;
					int ydir = aligntop ? 1 : -1;
					
					ctx.MoveTo(region.X + 2 * xdir, region.Y + 2 * ydir);
					d_gridColor.Set(ctx);
				
					Pango.CairoHelper.ShowLayout(ctx, layout);
					
					List<Rectangle> lst;

					if (!d_labelRegions.TryGetValue(labeled, out lst))
					{
						lst = new List<Rectangle>();
						d_labelRegions[labeled] = lst;
					}

					lst.Add(region);
					lastRegion = region;
				}				
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
		
		private void DrawOverlayBuffer(Cairo.Context ctx)
		{
			SetAntialias(ctx);

			// And then the axis labels
			ctx.Save();
			DrawAllXLabels(ctx);
			ctx.Restore();
			
			ctx.Save();
			DrawAllYLabels(ctx);
			ctx.Restore();
			
			// Paint label
			ctx.Save();
			DrawLabels(ctx);
			ctx.Restore();
		}
		
		private bool HasOverlayBuffer
		{
			get
			{
				return d_overlayBuffer != null && d_overlayBuffer.Content == Cairo.Content.ColorAlpha;
			}
		}
		
		private void DrawOverlay(Cairo.Context ctx)
		{
			ctx.Save();
			
			if (HasOverlayBuffer)
			{
				ctx.Save();
				ctx.SetSourceSurface(d_overlayBuffer, (int)d_dimensions.X, (int)d_dimensions.Y);
				ctx.Paint();
				ctx.Restore();
			}
			else
			{
				DrawOverlayBuffer(ctx);
			}
			
			if (d_showRuler)
			{
				DrawRuler(ctx);
			}
			
			if (d_showBox)
			{
				d_axisColor.Set(ctx);
				
				ctx.LineWidth = 1;
				ctx.Rectangle(0.5, 0.5, d_dimensions.Width - 1, d_dimensions.Height - 1);
				ctx.Stroke();
			}
			
			ctx.Restore();
		}
		
		public void DrawTo(Cairo.Context ctx, Rectangle dimensions)
		{
			ctx.Save();
			
			Rectangle dims = d_dimensions;
			d_dimensions = dimensions;
			
			ctx.Rectangle(dims.X, dims.Y, dims.Width, dims.Height);
			
			d_backgroundColor.Set(ctx);
			ctx.FillPreserve();
			ctx.Clip();

			SetAntialias(ctx);
			
			// First the axises (and ticks)
			ctx.Save();
			DrawXAxis(ctx);
			ctx.Restore();
			
			ctx.Save();
			DrawYAxis(ctx);
			ctx.Restore();

			// Then all the renderers
			ctx.Save();
			DrawRenderers(ctx);
			ctx.Restore();
			
			// And then the axis labels
			ctx.Save();
			DrawAllXLabels(ctx);
			ctx.Restore();
			
			ctx.Save();
			DrawAllYLabels(ctx);
			ctx.Restore();
			
			// Paint label
			ctx.Save();
			DrawLabels(ctx);
			ctx.Restore();
						
			bool showRuler = d_showRuler;
			bool showBox = d_showBox;
			
			d_showBox = true;
			d_showRuler = false;

			if (HasOverlayBuffer)
			{
				DrawOverlayBuffer(ctx);
			}
			
			DrawOverlay(ctx);
			
			ctx.Restore();
			
			d_showBox = showBox;

			d_showRuler = showRuler;
			d_dimensions = dims;
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
			
			d_backgroundColor.Set(ctx);
			ctx.Rectangle(d_dimensions.X - 0.5, d_dimensions.Y - 0.5, d_dimensions.Width + 1, d_dimensions.Height + 1);
			ctx.ClipPreserve();
			ctx.Fill();
			
			// First the axis (and ticks)
			ctx.Save();
			DrawXAxis(ctx);
			ctx.Restore();
				
			ctx.Save();
			DrawYAxis(ctx);
			ctx.Restore();
			
			ctx.Save();
			d_backbuffer[d_currentBuffer].Show(ctx, d_dimensions.X, d_dimensions.Y);
			ctx.Restore();

			DrawOverlay(ctx);
		}
		
		public void LookAt(Rectangle rectangle)
		{
			d_xaxis.Freeze();
			d_yaxis.Freeze();
			
			XAxis.Update(rectangle.X, rectangle.X + rectangle.Width);
			YAxis.Update(rectangle.Y, rectangle.Y + rectangle.Height);
			
			d_xaxis.Thaw();
			d_yaxis.Thaw();
		}
		
		public void ZoomAt(Point pt, double factorX, double factorY)
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
		
		public void MoveBy(Point pt)
		{
			d_xaxis.Freeze();
			d_yaxis.Freeze();
			
			XAxis.Update(d_xaxis.Min + pt.X, d_xaxis.Max + pt.X);
			YAxis.Update(d_yaxis.Min + pt.Y, d_yaxis.Max + pt.Y);
			
			d_xaxis.Thaw();
			d_yaxis.Thaw();
		}
		
		public Point PixelToAxis(Point pt)
		{
			return new Point(d_xaxis.Min + pt.X * ((d_xaxis.Max - d_xaxis.Min) / d_dimensions.Width),
			                 d_yaxis.Min + (d_dimensions.Height - pt.Y) * ((d_yaxis.Max - d_yaxis.Min) / d_dimensions.Height));
		}
		
		public Point ScaleFromPixel(Point pt)
		{
			return new Point(pt.X * ((d_xaxis.Max - d_xaxis.Min) / d_dimensions.Width),
			                         pt.Y * ((d_yaxis.Max - d_yaxis.Min) / d_dimensions.Height));
		}
		
		public void UpdateAxis(Range xaxis, Range yaxis)
		{
			d_xaxis.Freeze();
			d_yaxis.Freeze();

			d_xaxis.Update(xaxis);
			d_yaxis.Update(yaxis);
			
			d_xaxis.Thaw();
			d_yaxis.Thaw();
		}
		
		public bool LabelHitTest(Point pos)
		{
			Renderers.Renderer renderer;
			
			return LabelHitTest(pos, out renderer);
		}
		
		public bool LabelHitTest(Point pos, out Renderers.Renderer renderer)
		{
			renderer = null;
			
			foreach (KeyValuePair<Renderers.ILabeled, List<Rectangle>> pair in d_labelRegions)
			{
				foreach (Rectangle r in pair.Value)
				{
					if (r.Contains(pos))
					{
						renderer = (Renderers.Renderer)pair.Key;
						return true;
					}
				}
			}
			
			return false;
		}
	}
}
