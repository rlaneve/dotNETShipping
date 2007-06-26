using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net;
using System.Xml;

namespace dotNETShipping.ShippingProviders
{
	public struct Contact
	{
		public readonly string PersonName;
		public readonly string CompanyName;
		public readonly string Department;
		public readonly string PhoneNumber;
		public readonly string PagerNumber;
		public readonly string FaxNumber;
		public readonly string EmailAddress;

		public Contact(string personName, string companyName, string department, string phoneNumber, string pagerNumber, string faxNumber, string emailAddress)
		{
			this.PersonName = personName;
			this.CompanyName = companyName;
			this.Department = department;
			this.PhoneNumber = phoneNumber;
			this.PagerNumber = pagerNumber;
			this.FaxNumber = faxNumber;
			this.EmailAddress = emailAddress;
		}
	}

	/// <summary>
	/// Provides rates from FedEx (Federal Express).
	/// </summary>
	public class FedExProvider : AbstractShippingProvider
	{
		public enum AvailableServices
		{
			PriorityOvernight = 1,
			StandardOvernight = 2,
			FirstOvernight = 4,
			SecondDay = 8,
			ExpressSaver = 16,
			OvernightFreight = 32,
			SecondDayFreight = 64,
			ExpressSaverFreight = 128,
			Ground = 256,
			HomeDelivery = 512,
			All = 1023
		}

		private const string url = "https://gateway.fedex.com/GatewayDC";
		private const int defaultTimeout = 10;

		private string _accountNumber;
		private string _meterNumber;
		private int _timeout;
		private Hashtable _serviceCodes = new Hashtable(11);
		private AvailableServices _services = AvailableServices.All;

		public FedExProvider(string accountNumber, string meterNumber) : this(accountNumber, meterNumber, FedExProvider.defaultTimeout)
		{}

		public FedExProvider(string accountNumber, string meterNumber, int timeout)
		{
			this._name = "FedEx";
			this._accountNumber = accountNumber;
			this._meterNumber = meterNumber;
			this._timeout = timeout;

			this.loadServiceCodes();
		}

		public AvailableServices Services
		{
			get
			{
				return this._services;
			}
			set
			{
				this._services = value;
			}
		}

		public override void GetRates()
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(FedExProvider.url);
			request.Method = "POST";
			request.Timeout = this._timeout * 1000;
			// Per the FedEx documentation, the "ContentType" should be "application/x-www-form-urlencoded".
			// However, using "text/xml; encoding=UTF-8" lets us avoid converting the byte array returned by
			// the buildRatesRequestMessage method and (so far) works just fine.
			request.ContentType = "text/xml; encoding=UTF-8"; //"application/x-www-form-urlencoded";
			byte[] bytes = this.buildRequestMessage(); //System.Text.Encoding.Convert(Encoding.UTF8, Encoding.ASCII, this.buildRequestMessage());
			request.ContentLength = bytes.Length;
			Stream stream = request.GetRequestStream();
			stream.Write(bytes, 0, bytes.Length);
			stream.Close();
			System.Diagnostics.Debug.WriteLine("Request Sent!", "FedEx");
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			XmlDocument xml = new XmlDocument();
			xml.LoadXml(new StreamReader(response.GetResponseStream()).ReadToEnd());
			this._shipment.rates.AddRange(this.parseResponseMessage(xml));
			response.Close();
		}

		private byte[] buildRequestMessage()
		{
			System.Diagnostics.Debug.WriteLine("Building Request...", "FedEx");

			Encoding utf8 = new UTF8Encoding(false);
			XmlTextWriter writer = new XmlTextWriter(new MemoryStream(2000), utf8);
			writer.WriteStartDocument();
			writer.WriteStartElement("FDXRateAvailableServicesRequest");
			writer.WriteAttributeString("xmlns:api", "http://www.fedex.com/fsmapi");
			writer.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
			writer.WriteAttributeString("xsi:noNamespaceSchemaLocation", "FDXRateRequest.xsd");
			writer.WriteStartElement("RequestHeader");
			writer.WriteElementString("CustomerTransactionIdentifier", "RateRequest");
			writer.WriteElementString("AccountNumber", this._accountNumber);
			writer.WriteElementString("MeterNumber", this._meterNumber);
			if (this._services != AvailableServices.All && ((this._services & AvailableServices.Ground) == AvailableServices.Ground || (this._services & AvailableServices.HomeDelivery) == AvailableServices.HomeDelivery))
				writer.WriteElementString("CarrierCode", "FDXG");
			else
				writer.WriteElementString("CarrierCode", "FDXE");
			writer.WriteEndElement();
			writer.WriteElementString("DropoffType", "REGULARPICKUP");
			writer.WriteElementString("Packaging", "YOURPACKAGING");
			writer.WriteElementString("WeightUnits", "LBS");
			writer.WriteElementString("Weight", this._shipment.Packages[0].Weight.ToString("00.0"));
			writer.WriteElementString("ListRate", "1");
			writer.WriteStartElement("OriginAddress");
			writer.WriteElementString("StateOrProvinceCode", this._shipment.OriginAddress.State);
			writer.WriteElementString("PostalCode", this._shipment.OriginAddress.PostalCode);
			writer.WriteElementString("CountryCode", this._shipment.OriginAddress.CountryCode);
			writer.WriteEndElement();
			writer.WriteStartElement("DestinationAddress");
			writer.WriteElementString("StateOrProvinceCode", this._shipment.DestinationAddress.State);
			writer.WriteElementString("PostalCode", this._shipment.DestinationAddress.PostalCode);
			writer.WriteElementString("CountryCode", this._shipment.DestinationAddress.CountryCode);
			writer.WriteEndElement();
			writer.WriteStartElement("Payment");
			writer.WriteElementString("PayorType", "SENDER");
			writer.WriteEndElement();
			writer.WriteStartElement("Dimensions");
			writer.WriteElementString("Units", "IN");
			writer.WriteElementString("Length", this._shipment.Packages[0].Length.ToString());
			writer.WriteElementString("Width", this._shipment.Packages[0].Width.ToString());
			writer.WriteElementString("Height", this._shipment.Packages[0].Height.ToString());
			writer.WriteEndElement();
			writer.WriteElementString("PackageCount", "1");
			writer.WriteEndDocument();

			writer.Flush();
			byte[] buffer = new byte[writer.BaseStream.Length];
			writer.BaseStream.Position = 0;
			writer.BaseStream.Read(buffer, 0, buffer.Length);
			writer.Close();

			return buffer;
		}

		internal List<Rate> parseResponseMessage(XmlDocument response)
		{
			System.Diagnostics.Debug.WriteLine(response.OuterXml);
			List<Rate> rates = new List<Rate>();

			XmlNodeList nodesEntries = response.SelectNodes("/FDXRateAvailableServicesReply/Entry");
			foreach(XmlNode nodeEntry in nodesEntries)
			{
				string rateName = nodeEntry.SelectSingleNode("Service").InnerText;
				if (!this.serviceWasRequested(rateName))
					continue;
				string rateDesc = this._serviceCodes[rateName].ToString();
				XmlNode rateNode = (this.ApplyDiscounts ? nodeEntry.SelectSingleNode("EstimatedCharges/DiscountedCharges/NetCharge") : nodeEntry.SelectSingleNode("EstimatedCharges/ListCharges/NetCharge"));
				decimal totalCharges = Decimal.Parse(rateNode.InnerText);
				DateTime deliveryDate = DateTime.Now;
				if (nodeEntry.SelectSingleNode("DeliveryDate") != null)
					deliveryDate = this.getDeliveryDateTime(rateName, DateTime.Parse(nodeEntry.SelectSingleNode("DeliveryDate").InnerText));
				else if (nodeEntry.SelectSingleNode("TimeInTransit") != null)
					deliveryDate = this.getDeliveryDateTime(rateName, DateTime.Parse(DateTime.Now.AddDays(Convert.ToDouble(nodeEntry.SelectSingleNode("TimeInTransit").InnerText)).ToString("MM/dd/yyyy")));
				rates.Add(new Rate(this.Name, rateName, rateDesc, totalCharges, deliveryDate));
			}
			return rates;
		}

		private bool serviceWasRequested(string rateName)
		{
			if (!this._serviceCodes.ContainsKey(rateName))
				return false;
			AvailableService service = (AvailableService)this._serviceCodes[rateName];
			return (((int)this._services & service.EnumValue) == service.EnumValue);
		}
		
		private System.DateTime getDeliveryDateTime(string serviceCode, System.DateTime deliveryDate)
		{
			System.DateTime result = deliveryDate;

			switch(serviceCode)
			{
				case "PRIORITYOVERNIGHT":
					result = result.AddHours(10.5);
					break;
				case "FIRSTOVERNIGHT":
					result = result.AddHours(8.5);
					break;
				case "STANDARDOVERNIGHT":
					result = result.AddHours(15);
					break;
				case "FEDEX2DAY":
				case "FEDEXEXPRESSSAVER":
					result = result.AddHours(16.5);
					break;
				default: // no specific time, so use 11:59 PM to ensure correct sorting
					result = result.AddHours(23).AddMinutes(59);
					break;
			}
			return result;
		}

		private void loadServiceCodes()
		{
			this._serviceCodes.Add("PRIORITYOVERNIGHT", new AvailableService("Priority", 1));
			this._serviceCodes.Add("FEDEX2DAY", new AvailableService("2nd Day", 2));
			this._serviceCodes.Add("STANDARDOVERNIGHT", new AvailableService("Standard Overnight", 4));
			this._serviceCodes.Add("FIRSTOVERNIGHT", new AvailableService("First Overnight", 8));
			this._serviceCodes.Add("FEDEXEXPRESSSAVER", new AvailableService("Express Saver", 16));
			this._serviceCodes.Add("FEDEX1DAYFREIGHT", new AvailableService("Overnight Freight", 32));
			this._serviceCodes.Add("FEDEX2DAYFREIGHT", new AvailableService("2nd Day Freight", 64));
			this._serviceCodes.Add("FEDEX3DAYFREIGHT", new AvailableService("Express Saver Freight", 128));
			this._serviceCodes.Add("GROUNDHOMEDELIVERY", new AvailableService("Home Delivery", 256));
			this._serviceCodes.Add("FEDEXGROUND", new AvailableService("Ground", 512));
		}

		/// <summary>
		/// Subscribes an account number and returns a meter number.
		/// </summary>
		/// <param name="accountNumber">The FedEx account number to be subscribed.</param>
		/// <param name="contact">The contact information of the person subscribing.</param>
		/// <param name="address">The address information of the person subscribing.</param>
		/// <returns>A meter number.</returns>
		/// <remarks>
		/// This method only needs to be called once for a given account number.<br/>
		/// The returned meter number is then used when requesting rates or tracking information from FedEx.
		/// </remarks>
		public static string Subscribe(string accountNumber, Contact contact, dotNETShipping.Address address)
		{
			Encoding utf8 = new UTF8Encoding(false);
			XmlTextWriter writer = new XmlTextWriter(new MemoryStream(1000), utf8);
			writer.WriteStartDocument();
			writer.WriteStartElement("FDXSubscriptionRequest");
			writer.WriteAttributeString("xmlns:api", "http://www.fedex.com/fsmapi");
			writer.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
			writer.WriteAttributeString("xsi:noNamespaceSchemaLocation", "FDXSubscriptionRequest.xsd");
			writer.WriteStartElement("RequestHeader");
			writer.WriteElementString("CustomerTransactionIdentifier", "SubscriptionRequest");
			writer.WriteElementString("AccountNumber", accountNumber);
			writer.WriteEndElement();
			writer.WriteStartElement("Contact");
			writer.WriteElementString("PersonName", contact.PersonName);
			writer.WriteElementString("CompanyName", contact.CompanyName);
			writer.WriteElementString("Department", contact.Department);
			writer.WriteElementString("PhoneNumber", contact.PhoneNumber.Replace("-", "").Replace("(", "").Replace(")", ""));
			if(contact.PagerNumber.Length > 0)
				writer.WriteElementString("PagerNumber", contact.PagerNumber.Replace("-", "").Replace("(", "").Replace(")", ""));
			if(contact.FaxNumber.Length > 0)
				writer.WriteElementString("FaxNumber", contact.FaxNumber.Replace("-", "").Replace("(", "").Replace(")", ""));
			writer.WriteElementString("E-MailAddress", contact.EmailAddress);
			writer.WriteEndElement();
			writer.WriteStartElement("Address");
			writer.WriteElementString("Line1", address.Line1);
			writer.WriteElementString("Line2", address.Line2);
			writer.WriteElementString("City", address.City);
			writer.WriteElementString("StateOrProvinceCode", address.State);
			writer.WriteElementString("PostalCode", address.PostalCode);
			writer.WriteElementString("CountryCode", address.CountryCode);
			writer.WriteEndElement();
			writer.WriteEndDocument();

			writer.Flush();
			byte[] buffer = new byte[writer.BaseStream.Length];
			writer.BaseStream.Position = 0;
			writer.BaseStream.Read(buffer, 0, buffer.Length);
			string message = System.Text.Encoding.UTF8.GetString(buffer);
			writer.Close();
			System.Diagnostics.Debug.WriteLine(message);

			string temp = "";
			string meterNumber = "";
			try
			{
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(FedExProvider.url);
				request.Method = "POST";
				request.ContentType = "application/x-www-form-urlencoded";
				request.Timeout = 10 * 1000;
				byte[] bytes = System.Text.Encoding.ASCII.GetBytes(message);
				request.ContentLength = bytes.Length;
				Stream stream = request.GetRequestStream();
				stream.Write(bytes, 0, bytes.Length);
				stream.Close();
				System.Diagnostics.Debug.WriteLine("Request Sent!", "FedEx");
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				temp = new StreamReader(response.GetResponseStream()).ReadToEnd();
				response.Close();
				System.Diagnostics.Debug.WriteLine(temp);

				XmlDocument xDoc = new XmlDocument();
				xDoc.LoadXml(temp);
				XmlNode nodeMeterNumber = xDoc.SelectSingleNode("/FDXSubscriptionReply/MeterNumber");
				if(nodeMeterNumber != null)
					meterNumber = nodeMeterNumber.InnerText;
			}
			catch(System.Net.WebException ex)
			{
				System.Diagnostics.Debug.WriteLine(ex);
			}

			return meterNumber;
		}

		private struct AvailableService
		{
			public readonly string Name;
			public readonly int EnumValue;

			public AvailableService(string name, int enumValue)
			{
				this.Name = name;
				this.EnumValue = enumValue;
			}

			public override string ToString()
			{
				return this.Name;
			}
		}
	}
}
