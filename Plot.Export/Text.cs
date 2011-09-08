using System;
using System.Collections.Generic;
using System.IO;

namespace Plot.Export
{
	public class Text : Data
	{
		private string d_delimiter;

		public Text(string filename, string delimiter) : base(filename)
		{
			d_delimiter = delimiter;
		}
		
		public Text(string filename) : this(filename, "\t")
		{
		}
		
		public override void Export(params Graph[] graphs)
		{
			Export(true, graphs);
		}
		
		public void Export(bool headers, params Graph[] graphs)
		{
			base.Export(graphs);
			
			TextWriter writer = new StreamWriter(Filename);
			
			if (headers)
			{
				List<string> header = new List<string>();
				
				foreach (Renderers.Line r in this.Renderers)
				{
					header.Add(r.Label);
				}
				
				writer.WriteLine(String.Join(d_delimiter, header.ToArray()));
			}
			
			for (int r = 0; r < PlotData.Count; ++r)
			{
				for (int c = 0; c < PlotData[r].Count; ++c)
				{
					if (c != 0)
					{
						writer.Write(d_delimiter);
					}
					
					writer.Write(PlotData[r][c].ToString(System.Globalization.CultureInfo.InvariantCulture));
				}
				
				writer.WriteLine();
			}
			
			writer.Flush();
			writer.Close();
		}
	}
}

