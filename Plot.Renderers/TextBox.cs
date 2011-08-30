using System;

namespace Plot.Renderers
{
	public class TextBox : Box
	{
		private bool d_ismarkup;
		private string d_text;
		private bool d_recalculate;
		private Pango.FontDescription d_font;
		private Border<double> d_padding;

		public TextBox(string text)
		{
			d_text = text;
			d_ismarkup = false;
			d_recalculate = true;
			d_font = null;
		}
		
		public TextBox() : this("")
		{
		}
		
		public Border<double> Padding
		{
			get
			{
				return d_padding;
			}
			set
			{
				if (d_padding == value)
				{
					return;
				}
				
				if (d_padding != null)
				{
					d_padding.Changed -= PaddingChanged;
				}
				
				d_padding = value;
				
				if (d_padding != null)
				{
					d_padding.Changed += PaddingChanged;
				}
				
				PaddingChanged(d_padding, null);
			}
		}
		
		public bool IsMarkup
		{
			get
			{
				return d_ismarkup;
			}
			set
			{
				if (d_ismarkup != value)
				{
					d_ismarkup = value;
					d_recalculate = true;

					EmitChanged();
				}
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
					d_recalculate = true;

					EmitChanged();
				}
			}
		}
		
		public string Text
		{
			get
			{
				return d_text;
			}
			set
			{
				if (d_text != value)
				{
					d_text = value;
					d_recalculate = true;

					EmitChanged();
				}
			}
		}
		
		public string Markup
		{
			get
			{
				if (d_ismarkup)
				{
					return d_text;
				}
				else
				{
					return null;
				}
			}
			set
			{
				if (d_text != value || !d_ismarkup)
				{
					d_text = value;
					d_ismarkup = true;
					d_recalculate = true;
					
					EmitChanged();
				}
			}
		}
		
		public override void Render(Cairo.Context context, Point<double> scale)
		{
			if (d_recalculate)
			{
				//RecalculateSize();
			}

			base.Render(context, scale);
		}
		
		private void PaddingChanged(object source, EventArgs args)
		{
			d_recalculate = true;
			EmitChanged();
		}
	}
}

