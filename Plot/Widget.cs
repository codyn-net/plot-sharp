using System;
using System.Collections.Generic;

namespace Plot
{
	[Gtk.Binding(Gdk.Key.R, Gdk.ModifierType.ControlMask, "AutoAxis"),
	 Gtk.Binding(Gdk.Key.Escape, Gdk.ModifierType.None, "UndoLastZoom")]
	public class Widget : Gtk.DrawingArea
	{
		private struct Ranges
		{
			public Range<double> XRange;
			public Range<double> YRange;
			
			public Ranges(Range<double> xrange, Range<double> yrange)
			{
				XRange = new Range<double>(xrange);
				YRange = new Range<double>(yrange);
			}
		}

		private Graph d_graph;
		private Point<double> d_button;
		private Point<double> d_lastMove;
		private bool d_enableMove;
		private bool d_enableZoom;
		private bool d_enableSelect;
		private double d_zoomFactor;
		private Point<double> d_selectStart;
		private Point<double> d_selectEnd;
		private Gtk.ActionGroup d_popupActions;
		private Stack<Ranges> d_zoomstack;
		
		public delegate void PopulatePopupHandler(object source, Gtk.UIManager manager);
		public event PopulatePopupHandler PopulatePopup = delegate {};

		public Widget()
		{
			d_graph = new Graph();
			
			d_graph.RequestSurface += GraphRequestSurface;
			d_graph.RequestRedraw += GraphRequestRedraw;
			
			d_enableMove = true;
			d_enableZoom = true;
			d_enableSelect = true;

			d_zoomFactor = 1.25;
			d_zoomstack = new Stack<Ranges>();
			
			CanFocus = true;
			
			AddEvents((int)Gdk.EventMask.AllEventsMask);
			
			d_popupActions = new Gtk.ActionGroup("PopupActions");
			d_popupActions.Add(new Gtk.ToggleActionEntry[] {
				new Gtk.ToggleActionEntry("ActionShowRuler", null, "Ruler", null, null, OnActionShowRuler, d_graph.ShowRuler),
				new Gtk.ToggleActionEntry("ActionShowRulerAxis", null, "Ruler Axis", null, null, OnActionShowRulerAxis, d_graph.ShowRulerAxis),
				new Gtk.ToggleActionEntry("ActionShowTicks", null, "Ticks", null, null, OnActionShowTicks, true),
				new Gtk.ToggleActionEntry("ActionShowTickLabels", null, "Tick Labels", null, null, OnActionShowTickLabels, true),
				new Gtk.ToggleActionEntry("ActionShowGrid", null, "Grid", null, null, OnActionShowGrid, d_graph.ShowGrid),
				new Gtk.ToggleActionEntry("ActionKeepAspect", null, "Keep Aspect Ratio", null, null, OnActionKeepAspect, d_graph.KeepAspect),
				new Gtk.ToggleActionEntry("ActionShowLabels", null, "Labels", null, null, OnActionShowLabels, d_graph.ShowLabels),
				new Gtk.ToggleActionEntry("ActionShowBox", null, "Box", null, null, OnActionShowBox, d_graph.ShowBox),
				new Gtk.ToggleActionEntry("ActionShowAxis", null, "Axis", null, null, OnActionShowAxis, d_graph.ShowAxis),
				new Gtk.ToggleActionEntry("ActionSnapRuler", null, "Snap Ruler to Data", null, null, OnActionSnapRulerToData, d_graph.SnapRulerToData)
			});
			
			d_popupActions.Add(new Gtk.ActionEntry[] {
				new Gtk.ActionEntry("ActionAutoAxis", null, "Auto Axis", null, null, OnActionAutoAxis),
				new Gtk.ActionEntry("ActionExport", null, "Export", null, null, null),
				new Gtk.ActionEntry("ActionExportPdf", null, "Export to PDF", null, null, OnActionExportPdf),
				new Gtk.ActionEntry("ActionExportPs", null, "Export to PS", null, null, OnActionExportPs),
				new Gtk.ActionEntry("ActionExportSvg", null, "Export to SVG", null, null, OnActionExportSvg),
				new Gtk.ActionEntry("ActionExportPng", null, "Export to PNG", null, null, OnActionExportPng),
				new Gtk.ActionEntry("ActionShow", null, "Show", null, null, null),
			});
		}
		
		public bool EnableMove
		{
			get
			{
				return d_enableMove;
			}
			set
			{
				d_enableMove = value;
				
				if (!d_enableMove)
				{
					d_button = null;
					d_lastMove = null;
				}
			}
		}
		
		public bool EnableSelect
		{
			get
			{
				return d_enableSelect;
			}
			set
			{
				d_enableSelect = value;
				
				if (!d_enableSelect)
				{
					d_selectStart = null;
				}
			}
		}
		
		public bool EnableZoom
		{
			get
			{
				return d_enableZoom;
			}
			set
			{
				d_enableZoom = value;
			}
		}
		
		public double ZoomFactor
		{
			get
			{
				return d_zoomFactor;
			}
			set
			{
				d_zoomFactor = value;
			}
		}
		
		private void GraphRequestRedraw(object source, EventArgs args)
		{
			QueueDraw();
		}

		private void GraphRequestSurface(object source, RequestSurfaceArgs args)
		{
			using (Cairo.Context ctx = Gdk.CairoHelper.Create(GdkWindow))
			{
				Cairo.Surface surface = ctx.Target.CreateSimilar(ctx.Target.Content, args.Width, args.Height);
				args.Surface = surface;
			}
		}
		
		public Graph Graph
		{
			get
			{
				return d_graph;
			}
		}
		
		private bool Zoom(double x, double y, bool zoomin, Gdk.ModifierType mod)
		{
			if (!d_enableZoom)
			{
				return false;
			}

			Plot.Point<double> pt = new Plot.Point<double>(x, y);
			pt = d_graph.PixelToAxis(pt);
			
			double factor = zoomin ? d_zoomFactor : 1 / d_zoomFactor;
			double hasx = -1;
			double hasy = -1;

			Gdk.ModifierType zoomy = Gdk.ModifierType.ControlMask | Gdk.ModifierType.Mod1Mask;
			Gdk.ModifierType zoomx = Gdk.ModifierType.ControlMask;

			mod = mod & (zoomy | zoomx);
			
			if (mod == 0 || mod == zoomx)
			{
				hasx = 1;
			}
			
			if (mod == 0 || mod == zoomy)
			{
				hasy = 1;
			}
			
			d_graph.ZoomAt(pt, factor * hasx, factor * hasy);
			
			return true;
		}
		
		private bool SwitchRuler(bool up)
		{
			Renderers.Renderer lastrend = null;
			
			foreach (Renderers.Renderer renderer in d_graph.Renderers)
			{
				if (renderer.HasRuler)
				{
					lastrend = renderer;
					renderer.HasRuler = false;
				}
			}
			
			Renderers.Renderer beforecan = null;
			Renderers.Renderer firstcan = null;
			
			foreach (Renderers.Renderer renderer in d_graph.Renderers)
			{
				if (renderer == lastrend)
				{
					if (!up && beforecan != null)
					{
						beforecan.HasRuler = true;
						return true;
					}
					
					lastrend = null;
					continue;
				}

				if (renderer.CanRule)
				{
					if (firstcan == null)
					{
						firstcan = renderer;
					}

					beforecan = renderer;
					
					if (lastrend == null && !up)
					{
						renderer.HasRuler = true;
						return true;
					}
				}
			}
			
			if (firstcan != null && up)
			{
				firstcan.HasRuler = true;
			}
			else if (beforecan != null && !up)
			{
				beforecan.HasRuler = true;
			}
			
			return true;
		}
		
		protected override bool OnScrollEvent(Gdk.EventScroll evnt)
		{
			Gdk.ModifierType state = Gtk.Accelerator.DefaultModMask & evnt.State;

			if (state == Gdk.ModifierType.Mod1Mask)
			{
				return SwitchRuler(evnt.Direction == Gdk.ScrollDirection.Up);
			}
			else
			{
				return Zoom(evnt.X, evnt.Y, evnt.Direction == Gdk.ScrollDirection.Up, evnt.State);
			}
		}
		
		protected override bool OnLeaveNotifyEvent(Gdk.EventCrossing evnt)
		{
			d_graph.Ruler = null;
			GdkWindow.Cursor = null;

			return true;
		}
		
		protected override bool OnEnterNotifyEvent(Gdk.EventCrossing evnt)
		{
			Point<double> pt = new Point<double>(evnt.X, evnt.Y);

			d_graph.Ruler = pt;
			
			Renderers.Renderer renderer;
			
			if (d_graph.LabelHitTest(pt, out renderer))
			{
				GdkWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Hand2);
				d_graph.Ruler = null;
			}

			return true;
		}
		
		protected override bool OnMotionNotifyEvent(Gdk.EventMotion evnt)
		{
			Plot.Point<double> pt = new Plot.Point<double>(evnt.X, evnt.Y);

			if (d_graph.Ruler != null)
			{
				d_graph.Ruler.Move((int)evnt.X, (int)evnt.Y);
			}
			else
			{
				d_graph.Ruler = pt;
			}
			
			if (d_button != null)
			{			
				Plot.Point<double> move = d_graph.ScaleFromPixel(new Plot.Point<double>(evnt.X - d_button.X, evnt.Y - d_button.Y));
			
				d_graph.MoveBy(new Plot.Point<double>(-move.X + d_lastMove.X, move.Y - d_lastMove.Y));
				d_lastMove = move;
				
				return true;
			}
			
			if (d_selectStart != null)
			{
				d_selectEnd = pt;
				QueueDraw();
				
				return true;
			}
			
			Renderers.Renderer renderer;
			
			if (d_graph.LabelHitTest(pt, out renderer))
			{
				GdkWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Hand2);
				d_graph.Ruler = null;
			}
			else
			{
				GdkWindow.Cursor = null;
			}

			return false;
		}
		
		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated(allocation);
			
			d_graph.Dimensions.Resize(allocation.Width, allocation.Height);
		}
		
		private Rectangle<double> NormalizedSelection
		{
			get
			{
				if (!ValidSelection)
				{
					return null;
				}
				
				return new Rectangle<double>(Math.Min(d_selectStart.X, d_selectEnd.X),
				                             Math.Min(d_selectStart.Y, d_selectEnd.Y),
				                             Math.Abs(d_selectEnd.X - d_selectStart.X),
				                             Math.Abs(d_selectEnd.Y - d_selectStart.Y));
			}
		}
		
		private bool ValidSelection
		{
			get
			{
				if (d_selectStart == null || d_selectEnd == null)
				{
					return false;
				}

				return !(d_selectStart.X == d_selectEnd.X || d_selectStart.Y == d_selectEnd.Y);
			}
		}
		
		private Gtk.ToggleAction PopupToggleAction(string name)
		{
			return (Gtk.ToggleAction)d_popupActions.GetAction(name);
		}
		
		private IEnumerable<Ticks> ForeachTicks()
		{
			yield return d_graph.XTicks;
			yield return d_graph.YTicks;
		}
		
		private bool DetermineShowTicks(out bool ret)
		{
			ret = true;
			bool first = true;

			foreach (Ticks ticks in ForeachTicks())
			{
				if (first)
				{
					ret = ticks.Visible;
					first = false;
				}
				else
				{
					if (ret != ticks.Visible)
					{
						return false;
					}
				}
			}
			
			return !first;
		}
		
		private bool DetermineShowTickLabels(out bool ret)
		{
			ret = true;
			bool first = true;

			foreach (Ticks ticks in ForeachTicks())
			{
				if (first)
				{
					ret = ticks.ShowLabels;
					first = false;
				}
				else
				{
					if (ret != ticks.ShowLabels)
					{
						return false;
					}
				}
			}
			
			return !first;
		}
		
		private void DoPopup(Gdk.EventButton evnt)
		{
			Gtk.UIManager manager = new Gtk.UIManager();
			
			manager.InsertActionGroup(d_popupActions, 0);
			
			manager.AddUiFromResource("Plot.Plot.menu.xml");
			
			PopupToggleAction("ActionShowRuler").Active = d_graph.ShowRuler;
			PopupToggleAction("ActionShowRulerAxis").Active = d_graph.ShowRulerAxis;
			PopupToggleAction("ActionShowGrid").Active = d_graph.ShowGrid;
			PopupToggleAction("ActionShowLabels").Active = d_graph.ShowLabels;
			PopupToggleAction("ActionKeepAspect").Active = d_graph.KeepAspect;
			PopupToggleAction("ActionShowBox").Active = d_graph.ShowBox;
			PopupToggleAction("ActionShowAxis").Active = d_graph.ShowAxis;
			PopupToggleAction("ActionSnapRuler").Active = d_graph.SnapRulerToData;
			
			bool ret;
			
			if (DetermineShowTicks(out ret))
			{
				PopupToggleAction("ActionShowTicks").Active = ret;
			}
			
			if (DetermineShowTickLabels(out ret))
			{
				PopupToggleAction("ActionShowTickLabels").Active = ret;
			}
			
			PopulatePopup(this, manager);
			
			Gtk.Menu menu = (Gtk.Menu)manager.GetWidget("/popup");

			menu.Popup(null, null, null, evnt != null ? evnt.Button : 0, evnt != null ? evnt.Time : 0);
		}
		
		protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
		{
			Gdk.ModifierType mod = evnt.State & Gtk.Accelerator.DefaultModMask;
			
			if (mod != 0)
			{
				return false;
			}

			if (d_enableMove && evnt.Button == 2)
			{
				d_button = new Plot.Point<double>(evnt.X, evnt.Y);
				d_lastMove = new Plot.Point<double>(0, 0);
				
				d_zoomstack.Push(new Ranges(d_graph.XAxis, d_graph.YAxis));
				
				return true;
			}
			else if (d_enableSelect && evnt.Button == 1)
			{
				d_selectStart = new Point<double>(evnt.X, evnt.Y);
			}
			else if (evnt.Button == 3)
			{
				DoPopup(evnt);
			}
			
			return false;
		}
		
		protected override bool OnButtonReleaseEvent(Gdk.EventButton evnt)
		{
			if (d_button != null && evnt.Button == 2)
			{
				d_button = null;
				d_lastMove = null;
			}
			else if (d_selectStart != null)
			{
				if (evnt.Button == 1 && ValidSelection)
				{
					Rectangle<double> r = NormalizedSelection;

					Point<double> pt1 = d_graph.PixelToAxis(new Point<double>(r.X, r.Y));
					Point<double> pt2 = d_graph.PixelToAxis(new Point<double>(r.X + r.Width, r.Y + r.Height));
					
					d_zoomstack.Push(new Ranges(d_graph.XAxis, d_graph.YAxis));
					
					d_graph.UpdateAxis(new Range<double>(pt1.X, pt2.X),
					                   new Range<double>(pt2.Y, pt1.Y));
				}
				else if (evnt.Button == 1)
				{
					Renderers.Renderer renderer;

					if (d_graph.LabelHitTest(new Point<double>(evnt.X, evnt.Y), out renderer))
					{
						renderer.HasRuler = !renderer.HasRuler;
					}
				}

				d_selectStart = null;
				d_selectEnd = null;
				
				QueueDraw();
			}

			return false;
		}
		
		private void DrawSelection(Cairo.Context ctx)
		{
			if (!ValidSelection)
			{
				return;
			}
			
			ctx.LineWidth = 1;

			ctx.Rectangle(d_selectStart.X + 0.5, d_selectStart.Y + 0.5, d_selectEnd.X - d_selectStart.X, d_selectEnd.Y - d_selectStart.Y);
			
			ctx.SetSourceRGBA(d_graph.RulerColor.R, d_graph.RulerColor.G, d_graph.RulerColor.B, d_graph.RulerColor.A * 0.2);
			ctx.FillPreserve();
			
			d_graph.RulerColor.Set(ctx);
			ctx.Stroke();
		}
		
		protected override bool OnExposeEvent(Gdk.EventExpose evnt)
		{
			base.OnExposeEvent(evnt);
			
			using (Cairo.Context ctx = Gdk.CairoHelper.Create(evnt.Window))
			{
				Gdk.CairoHelper.Rectangle(ctx, evnt.Area);
				ctx.Clip();
				
				ctx.Save();
				d_graph.Draw(ctx);
				ctx.Restore();
			
				if (d_selectStart != null && d_selectEnd != null)
				{
					DrawSelection(ctx);
				}
			}
			
			return true;
		}
		
		protected override void OnStyleSet(Gtk.Style previous_style)
		{
			base.OnStyleSet(previous_style);
			
			d_graph.Font = Style.FontDescription;
		}
		
		protected override bool OnPopupMenu()
		{
			return true;
		}
		
		private void UndoLastZoom()
		{
			if (d_zoomstack.Count == 0)
			{
				return;
			}
			
			Ranges r = d_zoomstack.Pop();
			
			d_graph.UpdateAxis(r.XRange, r.YRange);
		}
		
		public void AutoAxis()
		{
			d_graph.UpdateAxis(d_graph.DataXRange.Widen(0.1), d_graph.DataYRange.Widen(0.1));
		}
		
		private void OnActionShowRuler(object source, EventArgs args)
		{
			Gtk.ToggleAction action = (Gtk.ToggleAction)source;
			
			d_graph.ShowRuler = action.Active;
		}
		
		private void OnActionShowRulerAxis(object source, EventArgs args)
		{
			Gtk.ToggleAction action = (Gtk.ToggleAction)source;
			
			d_graph.ShowRulerAxis = action.Active;
		}
		
		private void OnActionKeepAspect(object source, EventArgs args)
		{
			Gtk.ToggleAction action = (Gtk.ToggleAction)source;
			
			d_graph.KeepAspect = action.Active;
		}
		
		private void OnActionShowTicks(object source, EventArgs args)
		{
			Gtk.ToggleAction action = (Gtk.ToggleAction)source;
			
			foreach (Ticks ticks in ForeachTicks())
			{
				ticks.Visible = action.Active;
			}
			
			if (!action.Active)
			{
				d_graph.ShowGrid = false;
			}
		}
		
		private void OnActionShowTickLabels(object source, EventArgs args)
		{
			Gtk.ToggleAction action = (Gtk.ToggleAction)source;
			
			foreach (Ticks ticks in ForeachTicks())
			{
				ticks.ShowLabels = action.Active;
			}
		}
		
		private void OnActionShowGrid(object source, EventArgs args)
		{
			Gtk.ToggleAction action = (Gtk.ToggleAction)source;
			
			d_graph.ShowGrid = action.Active;
			
			if (action.Active)
			{
				foreach (Ticks ticks in ForeachTicks())
				{
					ticks.Visible = true;
				}
			}
		}
		
		private void OnActionAutoAxis(object source, EventArgs args)
		{
			AutoAxis();
		}
		
		private void OnActionShowLabels(object source, EventArgs args)
		{
			Gtk.ToggleAction action = (Gtk.ToggleAction)source;
			
			d_graph.ShowLabels = action.Active;
		}
		
		private void OnActionShowBox(object source, EventArgs args)
		{
			Gtk.ToggleAction action = (Gtk.ToggleAction)source;
			
			d_graph.ShowBox = action.Active;
		}
		
		private void OnActionShowAxis(object source, EventArgs args)
		{
			Gtk.ToggleAction action = (Gtk.ToggleAction)source;
			
			d_graph.ShowAxis = action.Active;
		}
		
		private void OnActionSnapRulerToData(object source, EventArgs args)
		{
			Gtk.ToggleAction action = (Gtk.ToggleAction)source;
			
			d_graph.SnapRulerToData = action.Active;
		}
		
		private delegate void ExporterHandler(string filename);
		
		private void ExportFilename(string extension, ExporterHandler handler)
		{
			Gtk.FileChooserDialog dlg = new Gtk.FileChooserDialog("Export graph as " + extension.ToUpper(),
			                                                      (Gtk.Window)Toplevel,
			                                                      Gtk.FileChooserAction.Save,
			                                                      Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
			                                                      Gtk.Stock.Save, Gtk.ResponseType.Ok);
			
			Gtk.FileFilter filter = new Gtk.FileFilter();
			
			filter.Name = extension.ToUpper();
			filter.AddPattern("*." + extension);

			dlg.AddFilter(filter);
			dlg.CurrentName = "export." + extension;
			dlg.DoOverwriteConfirmation = true;
		
			dlg.Response += delegate(object o, Gtk.ResponseArgs args) {
				if (args.ResponseId == Gtk.ResponseType.Ok)
				{
					handler(dlg.Filename);
				}
				
				dlg.Destroy();
			};
		
			dlg.Present();
		}
		
		private void OnActionExportPdf(object source, EventArgs args)
		{
			ExportFilename("pdf", delegate (string filename) {
				Export.Pdf exporter = new Export.Pdf(d_graph, filename);
				exporter.Export();
			});
		}
		
		private void OnActionExportPs(object source, EventArgs args)
		{
			ExportFilename("ps", delegate (string filename) {
				Export.Ps exporter = new Export.Ps(d_graph, filename);
				exporter.Export();
			});
		}
		
		private void OnActionExportSvg(object source, EventArgs args)
		{
			ExportFilename("svg", delegate (string filename) {
				Export.Svg exporter = new Export.Svg(d_graph, filename);
				exporter.Export();
			});
		}
		
		private void OnActionExportPng(object source, EventArgs args)
		{
			ExportFilename("png", delegate (string filename) {
				Export.Png exporter = new Export.Png(d_graph, filename);
				exporter.Export();
			});
		}
	}
}

