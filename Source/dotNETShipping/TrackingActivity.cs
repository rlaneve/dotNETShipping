using System;

namespace dotNETShipping
{
	/// <summary>
	/// Summary description for TrackingActivity.
	/// </summary>
	public class TrackingActivity
	{
		public readonly string TrackingNumber;
		public readonly string StatusDescription;
		public readonly string City;
		public readonly string State;
		public readonly string CountryCode;
		public readonly string ActivityDate;
		public readonly string ActivityTime;

		/// <summary>
		/// Creates a new instance of the <see cref="TrackingActivity"/> class.
		/// </summary>
		/// <param name="trackingNumber">The tracking number of the package.</param>
		/// <param name="statusDescription">The description of the activity status code.</param>
		/// <param name="city">The city from the tracking activity.</param>
		/// <param name="state">The state from the tracking activity.</param>
		/// <param name="countryCode">The country code from the tracking activity.</param>
		/// <param name="activityDate">The recorded date from the tracking activity.</param>
		/// <param name="activityTime">The recorded time from the tracking activity.</param>
		public TrackingActivity(string trackingNumber, string statusDescription, string city, string state, string countryCode, string activityDate, string activityTime)
		{
			this.TrackingNumber = trackingNumber;
			this.StatusDescription = statusDescription;
			this.City = city;
			this.State = state;
			this.CountryCode = countryCode;
			this.ActivityDate = activityDate;
			this.ActivityTime = activityTime;
		}

		public override string ToString()
		{
			return this.TrackingNumber + "\n\t" + this.StatusDescription + "\n\t" + this.City + "\n\t" + this.State + "\n\t" + this.CountryCode + "\n\t" + this.ActivityDate + "\n\t" + this.ActivityTime;
		}
	}
}
