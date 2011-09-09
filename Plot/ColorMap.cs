using System;
using System.Collections.Generic;
using Biorob.Math;

namespace Plot
{
	public class ColorMap : Changeable, IEnumerable<Color>
	{
		private static ColorMap s_default;

		private List<Color> d_colors;
		
		public ColorMap(IEnumerable<Color> colors)
		{
			d_colors = new List<Color>();
			
			Add(colors);
		}
		
		public ColorMap(params Color[] colors) : this((IEnumerable<Color>)colors)
		{
		}
		
		static ColorMap()
		{
			s_default = new ColorMap(
				new Color(0.1647, 0.3431, 0.5863),
				new Color(0.3784, 0.7137, 0.0549),
				new Color(0.7216, 0, 0),
				new Color(0.4098, 0.2608, 0.4412),
				new Color(0.8490, 0.7294, 0),
				new Color(0.8843, 0.4176, 0),
				new Color(0.6588, 0.4196, 0.0373),
				new Color(0.7784, 0.7922, 0.7627),
				new Color(0.2569, 0.2725, 0.2686)
			);
		}
		
		public ColorMap Copy()
		{
			return new ColorMap(d_colors);
		}
		
		public static ColorMap Default
		{
			get
			{
				return s_default;
			}
		}
		
		public void Add(IEnumerable<Color> colors)
		{
			foreach (Color color in colors)
			{
				Color cp = color.Copy();
				
				cp.Changed += OnColorChanged;
				d_colors.Add(cp);
			}
		}

		public void Add(params Color[] colors)
		{
			Add((IEnumerable<Color>)colors);
		}
		
		public void Remove(Color color)
		{
			if (d_colors.Contains(color))
			{
				color.Changed -= OnColorChanged;
				d_colors.Remove(color);
				
				EmitChanged();
			}
		}
		
		private void OnColorChanged(object source, EventArgs args)
		{
			EmitChanged();
		}
		
		public IEnumerator<Color> GetEnumerator()
		{
			return d_colors.GetEnumerator();
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return d_colors.GetEnumerator();
		}
		
		public Color this[int idx]
		{
			get
			{
				if (idx >= 0)
				{
					return d_colors[idx % d_colors.Count];
				}
				else
				{
					return d_colors[d_colors.Count + idx % (d_colors.Count - 1)];
				}
			}
		}
		
		public int Count
		{
			get
			{
				return d_colors.Count;
			}
		}		
	}
}

