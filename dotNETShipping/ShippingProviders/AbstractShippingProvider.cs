using System;

namespace dotNETShipping.ShippingProviders
{
	/// <summary>
	/// A base implementation of the <see cref="IShippingProvider"/> interface.
	/// All provider-specific classes should inherit from this class.
	/// </summary>
	public abstract class AbstractShippingProvider : IShippingProvider
	{
		internal string _name = null;
		private bool _applyDiscounts = RateManager.DEFAULT_ApplyDiscounts;
		internal Shipment _shipment = null;

		public string Name
		{
			get
			{
				return this._name;
			}
		}

		public bool ApplyDiscounts
		{
			get
			{
				return this._applyDiscounts;
			}
			set
			{
				this._applyDiscounts = value;
			}
		}
		
		public Shipment Shipment
		{
			get
			{
				return this._shipment;
			}
		}

		public virtual void GetRates()
		{
		
		}

		public virtual Shipment GetTrackingActivity(string trackingNumber)
		{
			return null;
		}
	}
}
