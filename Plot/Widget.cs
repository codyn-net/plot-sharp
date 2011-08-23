using System;
using System.Collections.Generic;

namespace Plot
{
	[Gtk.Binding(Gdk.Key.R, Gdk.ModifierType.ControlMask, "AutoAxis")]
	public class Widget : Gtk.DrawingArea
	{
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

		public Widget()
		{
			d_graph = new Graph();
			
			d_graph.RequestSurface += GraphRequestSurface;
			d_graph.RequestRedraw += GraphRequestRedraw;
			
			d_enableMove = true;
			d_enableZoom = true;
			d_enableSelect = true;

			d_zoomFactor = 1.25;
			
			CanFocus = true;
			
			AddEvents((int)Gdk.EventMask.AllEventsMask);
			
			d_popupActions = new Gtk.ActionGroup("PopupActions");
			d_popupActions.Add(new Gtk.ToggleActionEntry[] {
				new Gtk.ToggleActionEntry("ActionShowRuler", null, "Show Ruler", null, null, OnActionShowRuler, d_graph.ShowRuler),
				new Gtk.ToggleActionEntry("ActionShowTicks", null, "Show Ticks", null, null, OnActionShowTicks, true),
				new Gtk.ToggleActionEntry("ActionShowTickLabels", null, "Show Tick Labels", null, null, OnActionShowTickLabels, true),
				new Gtk.ToggleActionEntry("ActionShowGrid", null, "Show Grid", null, null, OnActionShowGrid, d_graph.ShowGrid),
				new Gtk.ToggleActionEntry("ActionKeepAspect", null, "Keep Aspect Ratio", null, null, OnActionKeepAspect, d_graph.KeepAspect)
			});
			
			d_popupActions.Add(new Gtk.ActionEntry[] {
				new Gtk.ActionEntry("ActionAutoAxis", null, "Auto Axis", null, null, OnActionAutoAxis)
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
			int numser = d_graph.Series.Length;

			if (numser == 0)
			{
				return false;
			}

			int nr = d_graph.RulerSeries + (up ? -1 : 1);
			
			if (nr < 0)
			{
				nr = numser;
			}
			else if (nr > numser)
			{
				nr = 0;
			}
			
			d_graph.RulerSeries = nr;
			return true;
		}
		
		protected override bool OnScrollEvent(Gdk.EventScroll evnt)
		{
			if ((evnt.State & Gdk.ModifierType.ControlMask) != 0)
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
			return true;
		}
		
		protected override bool OnEnterNotifyEvent(Gdk.EventCrossing evnt)
		{
			d_graph.Ruler = new Point<double>(evnt.X, evnt.Y);
			return true;
		}
		
		protected override bool OnMotionNotifyEvent(Gdk.EventMotion evnt)
		{
			if (d_graph.Ruler != null)
			{
				d_graph.Ruler.Move((int)evnt.X, (int)evnt.Y);
			}
			else
			{
				d_graph.Ruler = new Plot.Point<double>(evnt.X, evnt.Y);
			}
			
			if (d_button != null)
			{			
				Plot.Point<double> move = d_graph.ScaleFromPixel(new Plot.Point<double>(evnt.X - d_button.X, evnt.Y - d_button.Y));
			
				d_graph.MoveBy(new Plot.Point<double>(-move.X + d_lastMove.X, move.Y - d_lastMove.Y));
				d_lastMove = move;
			}
			
			if (d_selectStart != null)
			{
				d_selectEnd = new Point<double>(evnt.X, evnt.Y);
				QueueDraw();
			}

			return true;
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
			foreach (Ticks ticks in d_graph.XTicks)
			{
				yield return ticks;
			}
			
			foreach (Ticks ticks in d_graph.YTicks)
			{
				yield return ticks;
			}
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
			PopupToggleAction("ActionShowGrid").Active = d_graph.ShowGrid;
			PopupToggleAction("ActionKeepAspect").Active = d_graph.KeepAspect;
			
			bool ret;
			
			if (DetermineShowTicks(out ret))
			{
				PopupToggleAction("ActionShowTicks").Active = ret;
			}
			
			if (DetermineShowTickLabels(out ret))
			{
				PopupToggleAction("ActionShowTickLabels").Active = ret;
			}
			
			Gtk.Menu menu = (Gtk.Menu)manager.GetWidget("/popup");

			menu.Popup(null, null, null, evnt != null ? evnt.Button : 0, evnt != null ? evnt.Time : 0);
		}
		
		protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
		{
			if (d_enableMove && evnt.Button == 2)
			{
				d_button = new Plot.Point<double>(evnt.X, evnt.Y);
				d_lastMove = new Plot.Point<double>(0, 0);
				
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
					
					d_graph.UpdateAxis(new Range<double>(pt1.X, pt2.X),
					                   new Range<double>(pt2.Y, pt1.Y));
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
		
		public void AutoAxis()
		{
			d_graph.UpdateAxis(d_graph.DataXRange, d_graph.DataYRange);
		}
		
		private void OnActionShowRuler(object source, EventArgs args)
		{
			Gtk.ToggleAction action = (Gtk.ToggleAction)source;
			
			d_graph.ShowRuler = action.Active;
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
	}
}

