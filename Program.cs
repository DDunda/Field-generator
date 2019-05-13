using System;
using Tools;

namespace Fields
{
	class Program
	{
		static void Main(string[] args)
		{
		}
	}
	abstract class BasicPoint
	{
		public Vector2 position;
		public double magnitude;
		public abstract double GetForceMagnitude(Vector2 position2);
		public abstract Vector2 GetVectorForce(Vector2 position2);
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
			return magnitude / (position - position2).sqrMagnitude;
		}
		public override Vector2 GetVectorForce(Vector2 position2)
		{
			return this.GetForceMagnitude(position2) * (this.position - position2).normalised;
		}
	}
}