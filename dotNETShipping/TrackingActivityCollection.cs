using System;
using System.Collections;

namespace dotNETShipping
{
	/// <summary>
	/// Summary description for TrackingActivityCollection.
	/// </summary>
	public class TrackingActivityCollection : IEnumerable
	{
		private ArrayList _trackingActivities;

		public TrackingActivityCollection()
		{
			this._trackingActivities = new ArrayList();
		}

		public System.Collections.IEnumerator GetEnumerator()
		{
			return this._trackingActivities.GetEnumerator();
		}

		public int Add(TrackingActivity trackingActivity)
		{
			return this._trackingActivities.Add(trackingActivity);
		}

		public void Clear()
		{
			this._trackingActivities.Clear();
		}

		public TrackingActivity this[int index]
		{
			get
			{
				return (TrackingActivity)_trackingActivities[index];
			}
		}

		public int Count
		{
			get
			{
				return _trackingActivities.Count;
			}
		}
	}
}
