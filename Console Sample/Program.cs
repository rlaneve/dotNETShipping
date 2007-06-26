using System;
using System.Collections.Generic;
using System.Text;

namespace dotNETShipping.ConsoleSample
{
	class Program
	{
		static void Main(string[] args)
		{
			// You will need a license #, userid and password to utilize the UPS provider.
			string upsLicenseNumber = "";
			string upsUserId = "";
			string upsPassword = "";

			// You will need an account # and meter # to utilize the FedEx provider.
			string fedexAccountNumber = "";
			string fedexMeterNumber = "";

			// You will need a userid, password, account # and shipping key to utilize the Airborne provider.
			string airborneUserId = "";
			string airbornePassword = "";
			string airborneAccountNumber = "";
			string airborneShippingKey = "";

			// Setup package and destination/origin addresses
			dotNETShipping.Package package = new dotNETShipping.Package(12, 12, 16, 10, 1000);
			dotNETShipping.Address origin = new dotNETShipping.Address("Tampa", "FL", "33634", "US");
			dotNETShipping.Address destination = new dotNETShipping.Address("Newark", "DE", "19714", "US");

			// Create RateManager
			dotNETShipping.RateManager rateManager = new dotNETShipping.RateManager(true);

			// Add desired dotNETShippingProviders
			rateManager.AddProvider(new dotNETShipping.ShippingProviders.UPSProvider(upsLicenseNumber, upsUserId, upsPassword));

			// We don't want discounts from FedEx, so we need to create an object reference and set the "ApplyDiscounts" property.
			dotNETShipping.ShippingProviders.FedExProvider fedEx = new dotNETShipping.ShippingProviders.FedExProvider(fedexAccountNumber, fedexMeterNumber);
			fedEx.ApplyDiscounts = false;
			rateManager.AddProvider(fedEx);

			// We don't want all services from Airborne, so we need to create an object reference and set the "Services" property.
			dotNETShipping.ShippingProviders.AirborneProvider airborne = new dotNETShipping.ShippingProviders.AirborneProvider(airborneUserId, airbornePassword, airborneAccountNumber, airborneShippingKey);
			airborne.Services = dotNETShipping.ShippingProviders.AirborneProvider.AvailableServices.All ^ dotNETShipping.ShippingProviders.AirborneProvider.AvailableServices.Ground;
			rateManager.AddProvider(airborne);

			// Call GetRates()
			dotNETShipping.Shipment shipment = rateManager.GetRates(origin, destination, package);

			// Iterate through the rates returned
			foreach (dotNETShipping.Rate rate in shipment.Rates)
				System.Diagnostics.Debug.WriteLine(rate);
		}
	}
}
