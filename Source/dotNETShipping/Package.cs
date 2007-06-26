using System;

namespace dotNETShipping
{
	/// <summary>
	/// Summary description for Package.
	/// </summary>
	public class Package
	{
		public readonly decimal Length;
		public readonly decimal Width;
		public readonly decimal Height;
		public readonly decimal Weight;
		public readonly decimal InsuredValue;
		public readonly int Pounds;
		public readonly int Ounces;
		public string Service;
		public string Container;
		public bool Machinable;

		/// <summary>
		/// Creates a new package object.
		/// </summary>
		/// <param name="length">The length of the package, in inches.</param>
		/// <param name="width">The width of the package, in inches.</param>
		/// <param name="height">The height of the package, in inches.</param>
		/// <param name="weight">The weight of the package, in pounds.</param>
		/// <param name="insuredValue">The insured-value of the package, in dollars.</param>
		public Package(int length, int width, int height, int weight, decimal insuredValue) : this((decimal)length, (decimal)width, (decimal)height, (decimal)weight, insuredValue)
		{}

		/// <summary>
		/// Creates a new package object.
		/// </summary>
		/// <param name="length">The length of the package, in inches.</param>
		/// <param name="width">The width of the package, in inches.</param>
		/// <param name="height">The height of the package, in inches.</param>
		/// <param name="weight">The weight of the package, in pounds.</param>
		/// <param name="insuredValue">The insured-value of the package, in dollars.</param>
		public Package(decimal length, decimal width, decimal height, decimal weight, decimal insuredValue)
		{
			this.Length = length;
			this.Width = width;
			this.Height = height;
			this.Weight = weight;
			this.InsuredValue = insuredValue;
			this.Pounds = Convert.ToInt32(this.Weight - this.Weight % 1);
			decimal tempWeight = weight * 16;
			this.Ounces = Convert.ToInt32(Math.Ceiling((double)tempWeight - (double)this.Pounds * 16.0));
		}

		/// <summary>
		/// Creates a new package object with pounds and ounces specified.
		/// </summary>
		/// <param name="length">The length of the package, in inches.</param>
		/// <param name="width">The width of the package, in inches.</param>
		/// <param name="height">The height of the package, in inches.</param>
		/// <param name="pounds">Weight in pounds</param>
		/// <param name="ounces">Weight in ounces, uses as in 8 pounds 5 ounces.</param>
		/// <param name="insuredValue">The insured-value of the package, in dollars.</param>
		public Package(decimal length, decimal width, decimal height, int pounds, int ounces, decimal insuredValue)
		{
			this.Length = length;
			this.Width = width;
			this.Height = height;
			this.Weight = pounds + ounces/16;
			this.InsuredValue = insuredValue;
			this.Pounds = pounds;
			this.Ounces = ounces;
		}
	}
}
