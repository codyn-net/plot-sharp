using System;

namespace Plot.Renderers
{
	public interface ILabeled
	{
		string Label
		{
			get;
			set;
		}
		
		string LabelMarkup
		{
			get;
			set;
		}
	}
}

