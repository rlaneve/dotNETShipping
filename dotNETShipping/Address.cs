using System;

namespace dotNETShipping
{
	/// <summary>
	/// Summary description for Address.
	/// </summary>
	public class Address
	{
		public readonly string Line1;
		public readonly string Line2;
		public readonly string Line3;
		public readonly string City;
		public readonly string State;
		public readonly string PostalCode;
		public readonly string CountryCode;

		public Address(string city, string state, string postalCode, string countryCode) : this(null, null, null, city, state, postalCode, countryCode)
		{}

		public Address(string line1, string line2, string line3, string city, string state, string postalCode, string countryCode)
		{
			this.Line1 = line1;
			this.Line2 = line2;
			this.Line3 = line3;
			this.City = city;
			this.State = state;
			this.PostalCode = postalCode;
			this.CountryCode = countryCode;
		}
	}
}
