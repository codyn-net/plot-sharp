using System;

namespace Plot.Renderers
{
	public interface ILabeled
	{
		string XLabel
		{
			get;
			set;
		}
		
		string XLabelMarkup
		{
			get;
			set;
		}
		
		string YLabel
		{
			get;
			set;
		}
		
		string YLabelMarkup
		{
			get;
			set;
		}
	}
}

