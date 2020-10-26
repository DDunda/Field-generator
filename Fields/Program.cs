using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using Tools;
using Tools.EasyIO;
using System.Linq;
using System.Text.RegularExpressions;

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

		Vector2 GetForceAtPoint(Vector2 pos)
        {
			Vector2 ForceSum = Vector2.Zero;

			foreach(BasicPoint point in points)
				ForceSum += point.GetVectorForce(pos);

			return ForceSum;
        }

		// Maps screen space to world space
		Vector2 MapPoint(Vector2 screenPos)
        {
			Vector2 worldPos = screenPos - ((settings.imageSize - Vector2.One) / 2.0);
			worldPos.y *= -1;
			worldPos.x /= (settings.imageSize.x - 1);
			worldPos.y /= (settings.imageSize.y - 1);
			worldPos.x *= settings.windowSize.x;
			worldPos.y *= settings.windowSize.y;
			worldPos += settings.windowPosition;
			return worldPos;
		}

		void renderAscii()
		{
			UInt32 width = (UInt32)settings.imageSize.x;
			UInt32 height = (UInt32)settings.imageSize.y;

			char[][] forcefield = new char[height][];

			double section = 2 * Math.PI / 16;

			for (int y = 0; y < height; y++)
			{
				forcefield[y] = new char[width];
				for (int x = 0; x < width; x++)
				{
					Vector2 pos = MapPoint(new Vector2(x, y));
					Vector2 force = GetForceAtPoint(pos).normalised;

					char line = '.';
					if (force.size > 0)
					{
						double angle = Math.Atan2(force.y, force.x);

						line = '←';

						     if (angle >= -7 * section && angle < -5 * section) line = '↙';
						else if (angle >= -5 * section && angle < -3 * section) line = '↓';
						else if (angle >= -3 * section && angle < -1 * section) line = '↘';
						else if (angle >= -1 * section && angle <  1 * section) line = '→';
						else if (angle >=  1 * section && angle <  3 * section) line = '↗';
						else if (angle >=  3 * section && angle <  5 * section) line = '↑';
						else if (angle >=  5 * section && angle <  7 * section) line = '↖';

					}
					forcefield[y][x] = line;
				}
			}

			string[] lines = new string[height];
			for (int y = 0; y < height; y++) lines[y] = new string(forcefield[y]);

			string outputPath = $@"{settings.saveLocation}\Output.txt";
			EasyIO.Write(outputPath, lines);
		}

		void renderImage()
		{
			double minForce = 0;
			double maxForce = 0;

			UInt32 width = (UInt32)settings.imageSize.x;
			UInt32 height = (UInt32)settings.imageSize.y;

			Vector2[,] forcefield = new Vector2[width, height];

			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
				{
					Vector2 pos = MapPoint(new Vector2(x, y));
					Vector2 force = GetForceAtPoint(pos);

					if (force.size < minForce || minForce == 0) minForce = force.size;
					if (force.size > maxForce                 ) maxForce = force.size;

					forcefield[x, y] = force;
				}

			if (settings.maxForce != 0) maxForce = settings.maxForce;

			Bitmap image = new Bitmap((int)width, (int)height);

			bool logScale = settings.logFactor != 0;

			double a = settings.logFactor;
			double logBase = (1 + a) / a;

			for (int y = 0; y < height; y++)
				for (int x = 0; x < width; x++)
				{
					Vector2 force = forcefield[x, y];
					Vector2 forceDir = force.size == 0 || force.size >= maxForce ? Vector2.One / 2.0 : (force.normalised + Vector2.One) / 2.0;

					if (force.size > maxForce) force = force.normalised * maxForce;

					double scaledForce = (force.size - minForce) / (maxForce - minForce);
					if(logScale)
						scaledForce = Math.Log((scaledForce + a) / a, logBase);

					int R = (int)Math.Round(255 * forceDir.x);
					int G = (int)Math.Round(255 * forceDir.y);
					int B = (int)Math.Round(255 * scaledForce);

					image.SetPixel(x, y, Color.FromArgb(255, R, G, B));
                }
			if (!Directory.Exists(settings.saveLocation)) Directory.CreateDirectory(settings.saveLocation);

			string outputPath = $@"{settings.saveLocation}\Output.png";
			image.Save(outputPath, ImageFormat.Png);
		}

		void Load()
		{
			if (!File.Exists(settings.pointSource)) throw new FileNotFoundException($"\"{settings.pointSource}\" does not exist!");
			string[] output = EasyIO.Read(settings.pointSource).ToArray();
			string type = "";
			List<string> args = new List<string>();
			List<BasicPoint> newPoints = new List<BasicPoint>();

			BasicPoint point;

			foreach (string line in output)
            {
				if(BasicPoint.pointTypes.ContainsKey(line.Trim()))
                {
					if(type != "")
                    {
						point = null;
						switch(type)
                        {
							case "InverseSquarePoint":
								point = new InverseSquarePoint().ParseSaveData(args.ToArray());
									break;
						}
						if(point != null) newPoints.Add(point);

						args.Clear();
						type = "";
                    }
					type = line.Trim();
                }
				else args.Add(line);
            }

			point = null;
			switch (type)
			{
				case "InverseSquarePoint":
					point = new InverseSquarePoint().ParseSaveData(args.ToArray());
					break;
			}
			if (point != null) newPoints.Add(point);

			points = newPoints.ToArray();
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

			EasyIO.Write(settings.pointSource, Output.ToArray());
		}
	}

	class ImageSettings
	{
		public enum RenderMode
		{
			Image, Ascii
		}
		static string[] shortArgumentExceptions = { "source" }; // Arguments that don't need two components
		public Vector2 imageSize = new Vector2(50, 50); // Size in pixels or characters (w,h)
		public Vector2 windowPosition = Vector2.Zero; // Where the center is
		public Vector2 windowSize = Vector2.One; // Size of window in screenspace
		public RenderMode drawMode = RenderMode.Image; // How the output is rendered
		public string saveLocation = Directory.GetCurrentDirectory().ToString() + @"\Default"; // Where output is saved
		public string pointSource = ""; // Full directory of a save file to read from or write to
		public bool addPoints = false; // Whether or not to add points to the world from the console
		public bool saveOnExit = false; // Whether or not to save the world to the pointSource save file
		public double maxForce = 0;
		public double logFactor = 0;
		public static ImageSettings Parse(string[] args)
		{
			ImageSettings newSettings = new ImageSettings();
			foreach (string arg in args)
			{
				string[] argBits = SplitArgument(arg);
				if (argBits.Length != 2 && Array.IndexOf(shortArgumentExceptions, argBits[0]) == -1) throw new ArgumentException($"Incomplete argument ({arg})");
				switch (argBits[0])
				{
					case "imageWidth":
						newSettings.imageSize.x = Convert.ToUInt32(argBits[1]);
						break;
					case "imageHeight":
						newSettings.imageSize.y = Convert.ToUInt32(argBits[1]);
						break;
					case "x":
						newSettings.windowPosition.x = Convert.ToDouble(argBits[1]);
						break;
					case "y":
						newSettings.windowPosition.y = Convert.ToDouble(argBits[1]);
						break;
					case "windowWidth":
						newSettings.windowSize.x = Convert.ToDouble(argBits[1]);
						break;
					case "windowHeight":
						newSettings.windowSize.y = Convert.ToDouble(argBits[1]);
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
					case "maxForce":
						newSettings.maxForce = Convert.ToDouble(argBits[1]);
						break;
					case "logFactor":
						newSettings.logFactor = Convert.ToDouble(argBits[1]);
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
			if ((position - position2).sqrSize == 0) return 0;
			return constant / (position - position2).sqrSize;
		}
		public override Vector2 GetVectorForce(Vector2 position2)
		{
			if((this.position - position2).size == 0) return Vector2.Zero;
			return (this.position - position2).normalised * this.GetForceMagnitude(position2);
		}
		public override BasicPoint ParseSaveData(string[] args)
		{
			InverseSquarePoint newPoint = new InverseSquarePoint();
			foreach (string line in args)
			{
				string[] components = Regex.Replace(line.Trim(), @"\s+", " ").Split(' ');
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
				"InverseSquarePoint",
				$"  position {this.position.x} {this.position.y}",
				$"  constant {this.constant}",
				""
			};
		}
	}
}