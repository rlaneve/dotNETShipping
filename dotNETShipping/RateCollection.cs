using System;
using System.Collections;

namespace dotNETShipping
{
	/// <summary>
	/// Summary description for RateCollection.
	/// </summary>
	public class RateCollection : IEnumerable
	{
		private ArrayList _rates;

		public RateCollection()
		{
			this._rates = new ArrayList();
		}

		public System.Collections.IEnumerator GetEnumerator()
		{
			return this._rates.GetEnumerator();
		}

		public int Add(Rate rate)
		{
			return this._rates.Add(rate);
		}

		public void Clear()
		{
			this._rates.Clear();
		}

		public Rate this[int index]
		{
			get
			{
				return (Rate)_rates[index];
			}
		}

		public int Count
		{
			get
			{
				return _rates.Count;
			}
		}
	}
}
