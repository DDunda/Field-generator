using System;
using System.IO;
using Tools;

namespace Fields
{
	class Program
	{
		static void Main(string[] args)
		{
			ImageSettings settings = ImageSettings.Parse(args);
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
		public string pointSource = ""; // Full directory of a JSON file to read from or write to
		public bool addPoints = false; // Whether or not to add points to the world from the console
		public bool saveOnExit = true; // Whether or not to save the world to the pointSource JSON file
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
		public Vector2 position;
		public double magnitude;
		public abstract double GetForceMagnitude(Vector2 position2);
		public abstract Vector2 GetVectorForce(Vector2 position2);
	}

	class ElasticPoint : BasicPoint
	{
		public double naturalLength;
		public override double GetForceMagnitude(Vector2 position2)
		{
			return Math.Abs((position - position2).size - naturalLength) * magnitude;
		}
		public override Vector2 GetVectorForce(Vector2 position2)
		{
			return ((position - position2).size - naturalLength) * magnitude * (position2 - position).normalised;
		}
	}
	class InverseSquarePoint : BasicPoint
	{
		public InverseSquarePoint(Vector2 position, double magnitude)
		{
			this.position = position;
			this.magnitude = magnitude;
		}
		public override double GetForceMagnitude(Vector2 position2)
		{
			return magnitude / (position - position2).sqrSize;
		}
		public override Vector2 GetVectorForce(Vector2 position2)
		{
			return (this.position - position2).normalised * this.GetForceMagnitude(position2);
		}
	}
}