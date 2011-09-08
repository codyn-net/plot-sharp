using System;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.Text;
using System.Reflection;

namespace Plot
{
	[XmlRoot]
	public class Settings
	{
		public bool ShowAxis { get; set; }
		public bool ShowBox { get; set; }
		public bool ShowGrid { get; set; }
		public bool ShowRuler { get; set; }	
		public bool ShowLabels { get; set; }		
		public bool ShowRangeLabels { get; set; }		
		public bool ShowXTicks { get; set; }		
		public bool ShowYTicks { get; set; }		
		public bool ShowXTicksLabels { get; set; }	
		public bool ShowYTicksLabels { get; set; }		
		public bool KeepAspect { get; set; }
		public bool Antialias { get; set; }
		public bool SnapRulerToData { get; set; }
		public bool ShowRulerAxis { get; set; }
		public double AxisAspect { get; set; }
		public string AxisColor { get; set; }		
		public string BackgroundColor { get; set; }
		public string RulerColor { get; set; }
		public string RulerLabelColorsFg { get; set; }
		public string RulerLabelColorsBg { get; set; }
		public string AxisLabelColorsFg { get; set; }
		public string AxisLabelColorsBg { get; set; }
		public string GridColor { get; set; }
		public AxisMode XAxisMode { get; set; }
		public AxisMode YAxisMode { get; set; }
		public Point<double> AutoMargin { get; set; }
		public Range<double> XAxis { get; set; }
		public Range<double> YAxis { get; set; }
		
		public Settings()
		{
			Revert();
		}
		
		public void Revert()
		{
			ShowAxis = true;
			ShowLabels = true;
			ShowXTicks = true;
			ShowYTicks = true;
			ShowXTicksLabels = true;
			ShowYTicksLabels = true;
			KeepAspect = false;
			ShowRuler = true;
			ShowRulerAxis = true;
			
			XAxisMode = AxisMode.Auto;
			YAxisMode = AxisMode.Auto;
			
			AutoMargin = new Point<double>(0, 0.05);
			
			XAxis = new Range<double>(-1, 1);
			YAxis = new Range<double>(-1, 1);
			
			AxisAspect = 1;
			
			Antialias = true;
			SnapRulerToData = true;
			
			AxisColor = (new Color(0, 0, 0)).Hex;
			BackgroundColor = (new Color(1, 1, 1)).Hex;
			
			RulerColor = (new Color(0.5, 0.5, 0.5)).Hex;
			RulerLabelColorsBg = (new Color(1, 1, 1, 0.8)).Hex;
			RulerLabelColorsFg = (new Color(0.5, 0.5, 0.5)).Hex;
			GridColor = (new Color(0.95, 0.95, 0.95)).Hex;

			AxisLabelColorsFg = (new Color(0, 0, 0)).Hex;
			AxisLabelColorsBg = (new Color(1, 1, 1, 0.8)).Hex;
		}
		
		public Settings(Graph graph) : this()
		{
			Get(graph);
		}
		
		public static void Default(Graph graph)
		{
			(new Settings()).Set(graph);
		}
		
		private static XmlWriterSettings WriterSettings
		{
			get
			{
				XmlWriterSettings settings = new XmlWriterSettings();
			
				settings.Indent = true;
				settings.NewLineOnAttributes = false;
				settings.Encoding = new UTF8Encoding(false);
			
				return settings;
			}
		}
		
		public void Save(string filename)
		{
			XmlWriter xmlWriter = XmlTextWriter.Create(filename, WriterSettings);
			XmlSerializer serializer = new XmlSerializer(typeof(Settings));
			
			serializer.Serialize(xmlWriter, this);
			xmlWriter.Close();
		}
		
		public static Settings Load(string filename)
		{
			XmlSerializer serializer = new XmlSerializer(typeof(Settings));
			
			XmlReader xmlReader = XmlTextReader.Create(filename);
			
			Settings settings = (Settings)serializer.Deserialize(xmlReader);			
			xmlReader.Close();
			
			return settings;
		}
		
		public void Set(Graph graph)
		{
			graph.ShowAxis = ShowAxis;
			graph.ShowBox = ShowBox;
			graph.ShowGrid = ShowGrid;
			graph.ShowRuler = ShowRuler;
			graph.ShowRulerAxis = ShowRulerAxis;
			graph.ShowLabels = ShowLabels;
			graph.ShowRangeLabels = ShowRangeLabels;
			graph.XTicks.Visible = ShowXTicks;
			graph.YTicks.Visible = ShowYTicks;
			graph.XTicks.ShowLabels = ShowXTicksLabels;
			graph.YTicks.ShowLabels = ShowYTicksLabels;
			graph.KeepAspect = KeepAspect;
			graph.AxisAspect = AxisAspect;
			graph.Antialias = Antialias;
			graph.SnapRulerToData = SnapRulerToData;
			
			graph.XAxis.Update(XAxis);
			graph.YAxis.Update(YAxis);

			graph.XAxisMode = XAxisMode;
			graph.YAxisMode = YAxisMode;

			graph.AxisColor.Update(AxisColor);
			graph.BackgroundColor.Update(BackgroundColor);
			graph.RulerColor.Update(RulerColor);
			graph.RulerLabelColors.Fg.Update(RulerLabelColorsFg);
			graph.RulerLabelColors.Bg.Update(RulerLabelColorsBg);
			graph.AxisLabelColors.Fg.Update(AxisLabelColorsFg);
			graph.AxisLabelColors.Bg.Update(AxisLabelColorsBg);
			graph.GridColor.Update(GridColor);
			
			graph.AutoMargin.Move(AutoMargin);
		}
		
		public void Get(Graph graph)
		{
			ShowAxis = graph.ShowAxis;
			ShowBox = graph.ShowBox;
			ShowGrid = graph.ShowGrid;
			ShowRuler = graph.ShowRuler;
			ShowLabels = graph.ShowLabels;
			ShowRulerAxis = graph.ShowRulerAxis;

			ShowRangeLabels = graph.ShowRangeLabels;
			ShowXTicks = graph.XTicks.Visible;
			ShowYTicks = graph.YTicks.Visible;
			ShowXTicksLabels = graph.XTicks.ShowLabels;
			ShowYTicksLabels = graph.YTicks.ShowLabels;
			KeepAspect = graph.KeepAspect;
			AxisAspect = graph.AxisAspect;
			Antialias = graph.Antialias;
			SnapRulerToData = graph.SnapRulerToData;
			XAxisMode = graph.XAxisMode;
			YAxisMode = graph.YAxisMode;
			
			XAxis.Update(graph.XAxis);
			YAxis.Update(graph.YAxis);
			
			AxisColor = graph.AxisColor.Hex;
			BackgroundColor = graph.BackgroundColor.Hex;
			RulerColor = graph.RulerColor.Hex;
			RulerLabelColorsFg = graph.RulerLabelColors.Fg.Hex;
			RulerLabelColorsBg = graph.RulerLabelColors.Bg.Hex;
			AxisLabelColorsFg = graph.AxisLabelColors.Fg.Hex;
			AxisLabelColorsBg = graph.AxisLabelColors.Bg.Hex;
			GridColor = graph.GridColor.Hex;
			
			AutoMargin.Move(graph.AutoMargin);
		}
		
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			Settings other = obj as Settings;
			
			if (other == null)
			{
				return false;
			}
			
			foreach (PropertyInfo info in GetType().GetProperties())
			{
				if (!info.GetValue(this, new object[] {}).Equals(info.GetValue(other, new object[] {})))
				{
					return false;
				}
			}
			
			return true;
		}
		
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}

