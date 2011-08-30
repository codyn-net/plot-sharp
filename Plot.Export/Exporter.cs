using System;

namespace Plot.Export
{
	public abstract class Exporter
	{
		private Graph d_graph;
		private Rectangle<int> d_dimensions;

		public Exporter(Graph graph)
		{
			d_graph = graph;
			d_dimensions = new Rectangle<int>(0, 0, graph.Dimensions.Width, graph.Dimensions.Height);
		}
		
		public Graph Graph
		{
			get
			{
				return d_graph;
			}
		}
		
		public Rectangle<int> Dimensions
		{
			get
			{
				return d_dimensions;
			}
		}
		
		public abstract void Export();
	}
}

