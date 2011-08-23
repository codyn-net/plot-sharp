using System;

namespace Plot
{
	public class ColorFgBg : Changeable
	{
		private Color d_fg;
		private Color d_bg;

		public ColorFgBg()
		{
			d_fg = new Color(0, 0, 0, 1);
			d_bg = new Color(1, 1, 1, 0.8);
			
			d_fg.Changed += delegate {
				EmitChanged();
			};
			
			d_bg.Changed += delegate {
				EmitChanged();
			};
		}
		
		public Color Fg
		{
			get
			{
				return d_fg;
			}
		}
		
		public Color Bg
		{
			get
			{
				return d_bg;
			}
		}
	}
}

