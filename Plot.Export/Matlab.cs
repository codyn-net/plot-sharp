using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Plot.Export
{
	public class Matlab : Data
	{
		public Matlab(string filename) : base(filename)
		{
		}
		
		public override void Export(params Graph[] graphs)
		{
			base.Export(graphs);
			
			FileStream stream = new FileStream(Filename, FileMode.Create);
			BinaryWriter writer = new BinaryWriter(stream);
			
			string msg = "MATLAB 5.0 MAT-file, Created by libplot-sharp, Biorob, EPFL";
			msg = msg.PadRight(124);

			writer.Write(ASCIIEncoding.ASCII.GetBytes(msg));
			writer.Write((UInt16)0x0100);
			writer.Write(ASCIIEncoding.ASCII.GetBytes("IM"));

			Int32 cols;
			Int32 rows = PlotData.Count;
			
			if (PlotData.Count > 0)
			{
				cols = PlotData[0].Count;
			}
			else
			{
				cols = 0;
			}
			
			// Write the matrix type tag
			writer.Write((UInt32)14);
			
			// TODO: write size
			writer.Write((UInt32)(cols * rows + 7) * 8);

			// Array flags tag
			writer.Write((UInt32)6);
			writer.Write((UInt32)8);
			
			// Array flags contents
			writer.Write((UInt32)0x0006);
			writer.Write((UInt32)0);
			
			// Array dimensions tag
			writer.Write((UInt32)5);
			writer.Write((UInt32)8);
			
			// Array dimensions contents
			writer.Write((Int32)rows);
			writer.Write((Int32)cols);
			
			// Matrix name tag
			writer.Write((UInt32)1);
			writer.Write((UInt32)8);
			
			// Matrix name content
			writer.Write("data".ToCharArray());
			writer.Write(new byte[] {0, 0, 0, 0});
			
			// Data tag
			writer.Write((UInt32)9);
			writer.Write((UInt32)(cols * rows * 8));
			
			// Data contents		
			for (int c = 0; c < cols; ++c)
			{
				for (int r = 0; r < rows; ++r)
				{
					writer.Write(PlotData[r][c]);
				}
			}
						
			stream.Flush();
			stream.Close();
		}
	}
}


