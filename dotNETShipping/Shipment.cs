using System;

namespace dotNETShipping
{
	/// <summary>
	/// Summary description for Shipment.
	/// </summary>
	public class Shipment
	{
		public readonly Address OriginAddress;
		public readonly Address DestinationAddress;
		public readonly PackageCollection Packages;
		public readonly string TrackingNumber;
		
		private RateCollection _rates;
		private TrackingActivityCollection _trackingActivities;

		internal Shipment(string trackingNumber)
		{
			this.TrackingNumber = trackingNumber;
			this._trackingActivities = new TrackingActivityCollection();
		}

		public Shipment(Address originAddress, Address destinationAddress, Package package)
		{
			this.OriginAddress = originAddress;
			this.DestinationAddress = destinationAddress;
			this.Packages = new PackageCollection();
			this.Packages.Add(package);
			this._rates = new RateCollection();
		}

		public Shipment(Address originAddress, Address destinationAddress, PackageCollection packages)
		{
			this.OriginAddress = originAddress;
			this.DestinationAddress = destinationAddress;
			this.Packages = packages;
			this._rates = new RateCollection();
		}

		internal RateCollection rates
		{
			get
			{
				return this._rates;
			}
			set
			{
				this._rates = value;
			}
		}

		public RateCollection Rates
		{
			get
			{
				return this._rates;
			}
		}

		public TrackingActivityCollection TrackingActivities
		{
			get
			{
				return this._trackingActivities;
			}
		}
	}
}
