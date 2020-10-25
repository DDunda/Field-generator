using System;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using Tools;

namespace Fields
{
	class EntryClass
	{
		static void Main(string[] args)
		{
			new Thread(new Program().NonStaticMain).Start(args);
		}
	}

	class Program
	{
		public ImageSettings settings;
		public BasicPoint[] points;
		public Bitmap image;
		public void NonStaticMain(object Oargs)
		{
			string[] args = (string[])Oargs;
			settings = ImageSettings.Parse(args);
			if (settings.pointSource.Length != 0)
				Load();
			switch (settings.drawMode)
			{
				case ImageSettings.RenderMode.Ascii:
					renderAscii();
					break;
				case ImageSettings.RenderMode.Image:
					renderImage();
					break;
				default:
					throw new ApplicationException($"Undefined rendering mode ({settings.drawMode}: {settings.drawMode.ToString()})");
			}
			if (settings.pointSource.Length != 0)
				Save();
		}

		void renderAscii()
		{

		}

		void renderImage()
		{

		}

		void Load()
		{
			string[] output = EasyIO.Read(settings.pointSource);

		}

		void Save()
		{
			List<string> Output = new List<string>();
			foreach (BasicPoint point in points)
			{
				Output.AddRange(point.GetSaveData());
				Output.Add(""); // Empty line seperator
			}
			if (Output.Count > 0)
				Output.RemoveAt(Output.Count - 1); // Removes the trailing empty line

			EasyIO.Write(Output.ToArray(), settings.pointSource);
		}
	}

	class ImageSettings
	{
		public enum RenderMode
		{
			Image, Ascii
		}
		static string[] shortArgumentExceptions = { "source" }; // Arguments that don't need two components
		public Vector2 windowSize = new Vector2(50, 50); // Size in pixels or characters (w,h)
		public Vector2 windowPosition = Vector2.Zero; // Where the center is
		public Vector2 windowDilation = Vector2.One; // How the output is stretched
		public RenderMode drawMode = RenderMode.Ascii; // How the output is rendered
		public string saveLocation = Directory.GetCurrentDirectory().ToString() + @"\Default"; // Where output is saved
		public string pointSource = ""; // Full directory of a save file to read from or write to
		public bool addPoints = false; // Whether or not to add points to the world from the console
		public bool saveOnExit = true; // Whether or not to save the world to the pointSource save file
		public static ImageSettings Parse(string[] args)
		{
			ImageSettings newSettings = new ImageSettings();
			foreach (string arg in args)
			{
				string[] argBits = SplitArgument(arg);
				if (argBits.Length != 2 && Array.IndexOf(shortArgumentExceptions, argBits[0]) == -1) throw new ArgumentException($"Incomplete argument ({arg})");
				switch (argBits[0])
				{
					case "x":
						newSettings.windowPosition.x = Convert.ToDouble(argBits[1]);
						break;
					case "y":
						newSettings.windowPosition.y = Convert.ToDouble(argBits[1]);
						break;
					case "width":
						newSettings.windowSize.x = Convert.ToUInt32(argBits[1]);
						break;
					case "height":
						newSettings.windowSize.y = Convert.ToUInt32(argBits[1]);
						break;
					case "xScale":
						newSettings.windowDilation.x = Convert.ToDouble(argBits[1]);
						break;
					case "yScale":
						newSettings.windowDilation.y = Convert.ToDouble(argBits[1]);
						break;
					case "mode":
						newSettings.drawMode = (RenderMode)Convert.ToUInt32(argBits[1]);
						break;
					case "location":
						newSettings.saveLocation = argBits[1];
						break;
					case "source":
						if (argBits.Length == 1) newSettings.pointSource = "";
						else newSettings.pointSource = argBits[1];
						break;
					case "save":
						newSettings.saveOnExit = Convert.ToBoolean(argBits[1]);
						break;
					case "add":
						newSettings.addPoints = Convert.ToBoolean(argBits[1]);
						break;
					default:
						throw new ArgumentException($"\"{argBits[0]}\" is not a valid setting.");
				}
			}
			return newSettings;
		}
		static string[] SplitArgument(string arg)
		{
			int index = arg.IndexOf(':');
			if (index > 0)
			{
				var str1 = arg.Substring(0, index);
				var str2 = arg.Substring(index + 1);
				return new string[] { str1, str2 };
			}
			return new string[] { arg };
		}
	}

	abstract class BasicPoint
	{
		public static Dictionary<string, Type> pointTypes = new Dictionary<string, Type>() { { "ElasticPoint", typeof(ElasticPoint) }, { "InverseSquarePoint", typeof(InverseSquarePoint) } };
		public Vector2 position;
		public double constant;
		public abstract double GetForceMagnitude(Vector2 position2);
		public abstract Vector2 GetVectorForce(Vector2 position2);
		public abstract BasicPoint ParseSaveData(string[] args);
		public abstract string[] GetSaveData();
	}

	class ElasticPoint : BasicPoint
	{
		public double naturalLength;
		public ElasticPoint(Vector2 position, double magnitude, double length)
		{
			this.position = position;
			this.constant = magnitude;
			this.naturalLength = length;
		}
		public ElasticPoint()
		{
			this.position = Vector2.Zero;
			this.constant = 1;
			this.naturalLength = 1;
		}
		public override double GetForceMagnitude(Vector2 position2)
		{
			return Math.Abs((position - position2).size - naturalLength) * constant;
		}
		public override Vector2 GetVectorForce(Vector2 position2)
		{
			return ((position - position2).size - naturalLength) * constant * (position2 - position).normalised;
		}
		public override BasicPoint ParseSaveData(string[] args)
		{
			ElasticPoint newPoint = new ElasticPoint();
			foreach (string line in args)
			{
				string[] components = line.Trim().Split(' ');
				switch (components[0])
				{
					case "position":
						newPoint.position.x = double.Parse(components[1]);
						newPoint.position.y = double.Parse(components[2]);
						break;
					case "length":
						newPoint.naturalLength = double.Parse(components[1]);
						break;
					case "constant":
						newPoint.constant = double.Parse(components[1]);
						break;
					default:
						break;
				}
			}
			return newPoint;
		}
		public override string[] GetSaveData()
		{
			return new string[] {
				"ElasticPoint",
				$"  position {this.position.x} {this.position.y}",
				$"  length {this.naturalLength}",
				$"  constant {this.constant}",
				""
			};
		}
	}
	class InverseSquarePoint : BasicPoint
	{
		public InverseSquarePoint(Vector2 position, double magnitude)
		{
			this.position = position;
			this.constant = magnitude;
		}
		public InverseSquarePoint()
		{
			this.position = Vector2.Zero;
			this.constant = 1;
		}
		public override double GetForceMagnitude(Vector2 position2)
		{
			return constant / (position - position2).sqrSize;
		}
		public override Vector2 GetVectorForce(Vector2 position2)
		{
			return (this.position - position2).normalised * this.GetForceMagnitude(position2);
		}
		public override BasicPoint ParseSaveData(string[] args)
		{
			InverseSquarePoint newPoint = new InverseSquarePoint();
			foreach (string line in args)
			{
				string[] components = line.Trim().Split(' ');
				switch (components[0])
				{
					case "position":
						newPoint.position.x = double.Parse(components[1]);
						newPoint.position.y = double.Parse(components[2]);
						break;
					case "constant":
						newPoint.constant = double.Parse(components[1]);
						break;
					default:
						break;
				}
			}
			return newPoint;
		}
		public override string[] GetSaveData()
		{
			return new string[] {
				"ElasticPoint",
				$"  position {this.position.x} {this.position.y}",
				$"  constant {this.constant}",
				""
			};
		}
	}
}