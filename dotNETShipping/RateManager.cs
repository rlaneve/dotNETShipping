using System;
using System.Collections;
using System.Threading;

namespace dotNETShipping
{
	/// <summary>
	/// Responsible for coordinating the retrieval of rates from the specified providers for a specified shipment.
	/// </summary>
	public class RateManager
	{
		/// <summary>
		/// Default value for handling discounts is to apply them.
		/// </summary>
		public const bool DEFAULT_ApplyDiscounts = true;

		private ArrayList _providers = null;
		private bool _applyDiscounts = DEFAULT_ApplyDiscounts;

		/// <summary>
		/// Creates a new RateManager instance using the default for whether or not to apply discounts.
		/// </summary>
		public RateManager() : this(DEFAULT_ApplyDiscounts)
		{}

		/// <summary>
		/// Creates a new RateManager instance using the specified value for whether or not to apply discounts.
		/// </summary>
		/// <param name="applyDiscounts">Boolean value indicating whether or not to apply discounts. Default is defined by <see cref="DEFAULT_ApplyDiscounts"/>.</param>
		public RateManager(bool applyDiscounts)
		{
			this._providers = new ArrayList();
			this._applyDiscounts = applyDiscounts;
		}

		/// <summary>
		/// Adds the specified provider to be rated when <see cref="GetRates"/> is called.
		/// </summary>
		/// <param name="provider">A provider-specific implementation of <see cref="ShippingProviders.IShippingProvider"/>.</param>
		public void AddProvider(ShippingProviders.IShippingProvider provider)
		{
			this._providers.Add(provider);
		}

		/// <summary>
		/// Retrieves rates for all of the specified providers using the specified address and package information.
		/// </summary>
		/// <param name="originAddress">An instance of <see cref="Address"/> specifying the origin of the shipment.</param>
		/// <param name="destinationAddress">An instance of <see cref="Address"/> specifying the destination of the shipment.</param>
		/// <param name="package">An instance of <see cref="Package"/> specifying the package to be rated.</param>
		/// <returns>A <see cref="Shipment"/> instance containing all returned rates.</returns>
		public Shipment GetRates(Address originAddress, Address destinationAddress, Package package)
		{
			Shipment shipment = new Shipment(originAddress, destinationAddress, package);
			return this.getRates(ref shipment);
		}

		/// <summary>
		/// Retrieves rates for all of the specified providers using the specified address and packages information.
		/// </summary>
		/// <param name="originAddress">An instance of <see cref="Address"/> specifying the origin of the shipment.</param>
		/// <param name="destinationAddress">An instance of <see cref="Address"/> specifying the destination of the shipment.</param>
		/// <param name="packages">An instance of <see cref="PackageCollection"/> specifying the packages to be rated.</param>
		/// <returns>A <see cref="Shipment"/> instance containing all returned rates.</returns>
		public Shipment GetRates(Address originAddress, Address destinationAddress, PackageCollection packages)
		{
			Shipment shipment = new Shipment(originAddress, destinationAddress, packages);
			return this.getRates(ref shipment);
		}

		private Shipment getRates(ref Shipment shipment)
		{
			// create an ArrayList of threads, pre-sized to the number of providers.
			ArrayList threads = new ArrayList(this._providers.Count);
			// iterate through the providers.
			foreach(ShippingProviders.AbstractShippingProvider provider in this._providers)
			{
				// assign the shipment and ApplyDiscounts value to the provider.
				provider._shipment = shipment;
// setting ApplyDiscounts here is overriding provider-specific settings - commenting it out for now
//				if(this._applyDiscounts)
//					provider.ApplyDiscounts = true;
				// set the ThreadStart method for the thread to the provider's GetRates method.
				Thread thread = new Thread(new ThreadStart(provider.GetRates));
				// assign the thread the name of the provider (for debugging purposes).
				thread.Name = provider.Name;
				// start the thread.
				thread.Start();
				// add the thread to our ArrayList.
				threads.Add(thread);
			}
			// loop continuously until all threads have been removed.
			while(threads.Count > 0)
			{
				// loop through the threads (we can't use an iterator since we'll be deleting from the ArrayList).
				for(int x = (threads.Count - 1); x > -1; x--)
				{
					// check the ThreadState to see if it's Stopped.
					if(((Thread)threads[x]).ThreadState == ThreadState.Stopped)
					{
						// it's stopped, so we'll abort the thread and remove it from the ArrayList.
						((Thread)threads[x]).Abort();
						threads.RemoveAt(x);
					}
				}
				Thread.Sleep(1);
			}

			// return our Shipment instance.
			return shipment;
		}
	}
}
