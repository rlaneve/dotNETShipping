using System;

namespace dotNETShipping
{
	/// <summary>
	/// Summary description for Rate.
	/// </summary>
	public class Rate : IComparable
	{
		/// <summary>
		/// The <see cref="ShippingProviders.IShippingProvider"/> implementation which provided this rate.
		/// </summary>
		public readonly string Provider;
		/// <summary>
		/// The name of the rate, as specified by the provider.
		/// </summary>
		public readonly string Name;
		/// <summary>
		/// A description of the rate, as specified by the provider.
		/// </summary>
		public readonly string Description;
		/// <summary>
		/// The total cost of this rate.
		/// </summary>
		public readonly decimal TotalCharges;
		/// <summary>
		/// The guaranteed date and time of delivery for this rate.
		/// </summary>
		public readonly System.DateTime GuaranteedDelivery;

		/// <summary>
		/// Creates a new instance of the <see cref="Rate"/> class.
		/// </summary>
		/// <param name="provider">The name of the provider responsible for this rate.</param>
		/// <param name="name">The name of the rate.</param>
		/// <param name="description">A description of the rate.</param>
		/// <param name="totalCharges">The total cost of this rate.</param>
		/// <param name="delivery">The guaranteed date and time of delivery for this rate.</param>
		public Rate(string provider, string name, string description, decimal totalCharges, System.DateTime delivery)
		{
			this.Provider = provider;
			this.Name = name;
			this.Description = description;
			this.TotalCharges = totalCharges;
			this.GuaranteedDelivery = delivery;
		}

		public override string ToString()
		{
			return this.Provider + Environment.NewLine + "\t" + this.Name + Environment.NewLine + "\t" + this.Description + Environment.NewLine + "\t" + this.TotalCharges + Environment.NewLine + "\t" + this.GuaranteedDelivery;
		}

		#region IComparable Members
		public int CompareTo(object obj)
		{
			Rate rateB = (Rate)obj;
			return this.GuaranteedDelivery.CompareTo(rateB.GuaranteedDelivery);
		}
		#endregion
	}
}
