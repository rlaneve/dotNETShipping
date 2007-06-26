using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace dotNETShipping
{
	/// <summary>
	/// Summary description for Shipment.
	/// </summary>
	public class Shipment
	{
		public readonly Address OriginAddress;
		public readonly Address DestinationAddress;
		public ReadOnlyCollection<Package> Packages;
		public readonly string TrackingNumber;
		
		private List<Rate> _rates;
		private List<TrackingActivity> _trackingActivities;

		internal Shipment(string trackingNumber)
		{
			this.TrackingNumber = trackingNumber;
			this._trackingActivities = new List<TrackingActivity>();
		}

		public Shipment(Address originAddress, Address destinationAddress, Package package)
		{
			this.OriginAddress = originAddress;
			this.DestinationAddress = destinationAddress;
			List<Package> packages = new List<Package>();
			packages.Add(package);
			this.Packages = packages.AsReadOnly();
			this._rates = new List<Rate>();
		}

		public Shipment(Address originAddress, Address destinationAddress, List<Package> packages)
		{
			this.OriginAddress = originAddress;
			this.DestinationAddress = destinationAddress;
			this.Packages = packages.AsReadOnly();
			this._rates = new List<Rate>();
		}

		internal List<Rate> rates
		{
			get
			{
				return this._rates;
			}
		}

		public ReadOnlyCollection<Rate> Rates
		{
			get
			{
				return this._rates.AsReadOnly();
			}
		}

		internal List<TrackingActivity> trackingActivities
		{
			get
			{
				return this._trackingActivities;
			}
		}

		public ReadOnlyCollection<TrackingActivity> TrackingActivities
		{
			get
			{
				return this._trackingActivities.AsReadOnly();
			}
		}
	}
}
