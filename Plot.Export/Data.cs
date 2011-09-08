using System;
using System.Collections.Generic;

namespace Plot.Export
{
	public class Data
	{
		private string d_filename;
		private List<Renderers.Line> d_renderers;
		private List<List<double>> d_data;
		
		public Data(string filename)
		{
			d_filename = filename;
		}
		
		public string Filename
		{
			get
			{
				return d_filename;
			}
		}
		
		public List<Renderers.Line> Renderers
		{
			get
			{
				return d_renderers;
			}
		}
		
		public List<List<double>> PlotData
		{
			get
			{
				return d_data;
			}
		}
		
		public virtual void Export(params Graph[] graphs)
		{
			d_renderers = new List<Renderers.Line>();
			d_data = new List<List<double>>();
			
			foreach (Graph graph in graphs)
			{
				foreach (Renderers.Renderer rend in graph.Renderers)
				{
					Renderers.Line line = rend as Renderers.Line;
					
					if (line != null)
					{
						d_renderers.Add(line);
					}
				}
			}
			
			int idx = 0;
			
			while (true)
			{
				List<double> row = new List<double>(d_renderers.Count * 2);
				bool isempty = true;
				
				foreach (Renderers.Line line in d_renderers)
				{
					if (idx < line.Count)
					{
						isempty = false;
						row.Add(line[idx].X);
						row.Add(line[idx].Y);
					}
					else
					{
						row.Add(0);
						row.Add(0);
					}					
				}
				
				if (isempty)
				{
					break;
				}
				
				d_data.Add(row);
				++idx;
			}
		}
	}
}

