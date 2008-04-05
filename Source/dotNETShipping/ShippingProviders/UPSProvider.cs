using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace dotNETShipping.ShippingProviders
{
	/// <summary>
	/// Provides rates from UPS (United Parcel Service).
	/// </summary>
	public class UPSProvider : AbstractShippingProvider
	{
		// These values need to stay in sync with the values in the "loadServiceCodes" method.
		public enum AvailableServices
		{
			NextDayAir = 1,
			SecondDayAir = 2,
			Ground = 4,
			WorldwideExpress = 8,
			WorldwideExpedited = 16,
			Standard = 32,
			ThreeDaySelect = 64,
			NextDayAirSaver = 128,
			NextDayAirEarlyAM = 256,
			WorldwideExpressPlus = 512,
			SecondDayAirAM = 1024,
			ExpressSaver = 2048,
			All = 4095
		}

		private const string ratesUrl = "https://www.ups.com/ups.app/xml/Rate";
		private const string trackUrl = "https://www.ups.com/ups.app/xml/Track";// this is the test URL: "https://wwwcie.ups.com/ups.app/xml/Track"
		private const int defaultTimeout = 10;

		private string _licenseNumber;
		private string _userID;
		private string _password;
		private int _timeout;
		private Hashtable _serviceCodes = new Hashtable(12);
		private AvailableServices _services = AvailableServices.All;

		public UPSProvider(string licenseNumber, string userID, string password) : this(licenseNumber, userID, password, UPSProvider.defaultTimeout)
		{}

		public UPSProvider(string licenseNumber, string userID, string password, int timeout)
		{
			this._name = "UPS";
			this._licenseNumber = licenseNumber;
			this._userID = userID;
			this._password = password;
			this._timeout = timeout;

			loadServiceCodes();
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
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(UPSProvider.ratesUrl);
			request.Method = "POST";
			request.Timeout = this._timeout * 1000;
			// Per the UPS documentation, the "ContentType" should be "application/x-www-form-urlencoded".
			// However, using "text/xml; encoding=UTF-8" lets us avoid converting the byte array returned by
			// the buildRatesRequestMessage method and (so far) works just fine.
			request.ContentType = "text/xml; encoding=UTF-8"; //"application/x-www-form-urlencoded";
			byte[] bytes = this.buildRatesRequestMessage(); //System.Text.Encoding.Convert(Encoding.UTF8, Encoding.ASCII, this.buildRatesRequestMessage());
			request.ContentLength = bytes.Length;
			Stream stream = request.GetRequestStream();
			stream.Write(bytes, 0, bytes.Length);
			stream.Close();
			System.Diagnostics.Debug.WriteLine("Request Sent!", "UPS");
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			this.parseRatesResponseMessage(new StreamReader(response.GetResponseStream()).ReadToEnd());
			response.Close();
		}

		public override Shipment GetTrackingActivity(string trackingNumber)
		{
			Shipment shipment = new Shipment(trackingNumber);
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(UPSProvider.trackUrl);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.Timeout = this._timeout * 1000;
			byte[] bytes = System.Text.Encoding.ASCII.GetBytes(this.buildTrackingActivityRequestMessage(trackingNumber));
			request.ContentLength = bytes.Length;
			Stream stream = request.GetRequestStream();
			stream.Write(bytes, 0, bytes.Length);
			stream.Close();
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			this.parseTrackingActivityResponseMessage(ref shipment, new StreamReader(response.GetResponseStream()).ReadToEnd());
			response.Close();
			return shipment;
		}

		private byte[] buildRatesRequestMessage()
		{
			System.Diagnostics.Debug.WriteLine("Building Request...", "UPS");

			Encoding utf8 = new UTF8Encoding(false);
			XmlTextWriter writer = new XmlTextWriter(new MemoryStream(2000), utf8);
			writer.WriteStartDocument();
			writer.WriteStartElement("AccessRequest");
			writer.WriteAttributeString("lang", "en-US");
			writer.WriteElementString("AccessLicenseNumber", this._licenseNumber);
			writer.WriteElementString("UserId", this._userID);
			writer.WriteElementString("Password", this._password);
			writer.WriteEndDocument();
			writer.WriteStartDocument();
			writer.WriteStartElement("RatingServiceSelectionRequest");
			writer.WriteAttributeString("lang", "en-US");
			writer.WriteStartElement("Request");
			writer.WriteStartElement("TransactionReference");
			writer.WriteElementString("CustomerContext", "Rating and Service");
			writer.WriteElementString("XpciVersion", "1.0001");
			writer.WriteEndElement(); // </TransactionReference>
			writer.WriteElementString("RequestAction", "Rate");
			writer.WriteElementString("RequestOption", "Shop");
			writer.WriteEndElement(); // </Request>
			writer.WriteStartElement("PickupType");
			writer.WriteElementString("Code", "01");
			writer.WriteEndElement(); // </PickupType>
			writer.WriteStartElement("Shipment");
			writer.WriteStartElement("Shipper");
			writer.WriteStartElement("Address");
			writer.WriteElementString("PostalCode", this._shipment.OriginAddress.PostalCode);
			writer.WriteEndElement(); // </Address>
			writer.WriteEndElement(); // </Shipper>
			writer.WriteStartElement("ShipTo");
			writer.WriteStartElement("Address");
			writer.WriteElementString("PostalCode", this._shipment.DestinationAddress.PostalCode);
			writer.WriteEndElement(); // </Address>
			writer.WriteEndElement(); // </ShipTo>
			for (int i = 0; i < this._shipment.Packages.Count; i++)
			{
				writer.WriteStartElement("Package");
				writer.WriteStartElement("PackagingType");
				writer.WriteElementString("Code", "00");
				writer.WriteEndElement(); //</PackagingType>
				writer.WriteStartElement("PackageWeight");
				writer.WriteElementString("Weight", this._shipment.Packages[i].Weight.ToString());
				writer.WriteEndElement(); // </PackageWeight>
				writer.WriteStartElement("Dimensions");
				writer.WriteElementString("Length", this._shipment.Packages[i].Length.ToString());
				writer.WriteElementString("Width", this._shipment.Packages[i].Width.ToString());
				writer.WriteElementString("Height", this._shipment.Packages[i].Height.ToString());
				writer.WriteEndElement(); // </Dimensions>
				writer.WriteEndElement(); // </Package>
			}
			writer.WriteEndDocument();
			writer.Flush();
			byte[] buffer = new byte[writer.BaseStream.Length];
			writer.BaseStream.Position = 0;
			writer.BaseStream.Read(buffer, 0, buffer.Length);
			writer.Close();

			return buffer;
		}

		private string buildTrackingActivityRequestMessage(string trackingNumber)
		{
			System.Diagnostics.Debug.WriteLine("Building Request...", "UPS");
			string request = "";
			request += "<?xml version=\"1.0\"?>\r\n";
			request += "<AccessRequest xml:lang=\"en-US\">\r\n";
			request += "<AccessLicenseNumber>" + this._licenseNumber + "</AccessLicenseNumber>\r\n";
			request += "<UserId>" + this._userID + "</UserId>\r\n";
			request += "<Password>" + this._password + "</Password>\r\n";
			request += "</AccessRequest>\r\n";
			request += "<?xml version=\"1.0\"?>";

			XmlDocument xDoc = new XmlDocument();
			XmlNode xRoot = xDoc.AppendChild(xDoc.CreateElement("TrackRequest"));
			xRoot.Attributes.Append(xDoc.CreateAttribute("lang")).Value = "en-US";
			XmlNode xRequest = xRoot.AppendChild(xDoc.CreateElement("Request"));
			XmlNode xNode = xRequest.AppendChild(xDoc.CreateElement("TransactionReference"));
			xNode.AppendChild(xDoc.CreateElement("CustomerContext")).InnerText = "Tracking";
			xNode.AppendChild(xDoc.CreateElement("XpciVersion")).InnerText = "1.0001";
			xRequest.AppendChild(xDoc.CreateElement("RequestAction")).InnerText = "Track";
			xRequest.AppendChild(xDoc.CreateElement("RequestOption")).InnerText = "activity";
			
			string[] trackingNumberArray = trackingNumber.Split(new char[] {','});
			foreach (string tracking in trackingNumberArray)
			{
				xRoot.AppendChild(xDoc.CreateElement("TrackingNumber")).InnerText = tracking;
			}
			
			request += xDoc.OuterXml;
			System.Diagnostics.Debug.WriteLine(request.ToString());
			return request;
		}

		private void parseRatesResponseMessage(string response)
		{
			System.Diagnostics.Debug.WriteLine("UPS Response Received!");
			System.Diagnostics.Debug.WriteLine(response);
			XmlDocument xDoc = new XmlDocument();
			xDoc.LoadXml(response);
			XmlNodeList rates = xDoc.SelectNodes("/RatingServiceSelectionResponse/RatedShipment");
			foreach(XmlNode rate in rates)
			{
				string name = rate.SelectSingleNode("Service/Code").InnerText;
				AvailableService service;
				if(this._serviceCodes.ContainsKey(name))
					service = (AvailableService)this._serviceCodes[name];
				else
					continue;
				if(((int)this._services & service.EnumValue) != service.EnumValue)
					continue;
				string description = "";
				if(this._serviceCodes.ContainsKey(name))
					description = this._serviceCodes[name].ToString();
				decimal totalCharges = Convert.ToDecimal(rate.SelectSingleNode("TotalCharges/MonetaryValue").InnerText);
				System.DateTime delivery = System.DateTime.Parse("1/1/1900 12:00 AM");
				string date = rate.SelectSingleNode("GuaranteedDaysToDelivery").InnerText;
				if(date == "") // no gauranteed delivery date, so use MaxDate to ensure correct sorting
					date = System.DateTime.MaxValue.ToShortDateString();
				else
					date = System.DateTime.Now.AddDays(Convert.ToDouble(date)).ToShortDateString();
				string deliveryTime = rate.SelectSingleNode("ScheduledDeliveryTime").InnerText;
				if(deliveryTime == "") // no scheduled delivery time, so use 11:59:00 PM to ensure correct sorting
					date += " 11:59:00 PM";
				else
					date += " " + deliveryTime.Replace("Noon", "PM").Replace("P.M.", "PM").Replace("A.M.", "AM");
				if(date != "")
					delivery = System.DateTime.Parse(date);
				this._shipment.rates.Add(new Rate("UPS", name, description, totalCharges, delivery));
			}
		}

		private void parseTrackingActivityResponseMessage(ref Shipment shipment, string response)
		{
			System.Diagnostics.Debug.WriteLine("UPS Response Received!");
			System.Diagnostics.Debug.WriteLine(response);
			XmlDocument xDoc = new XmlDocument();
			xDoc.LoadXml(response);
			XmlNodeList tracks = xDoc.SelectNodes("/TrackResponse/Shipment/Package");
			foreach(XmlNode track in tracks)
			{
				string trackingNumber = track.SelectSingleNode("TrackingNumber").InnerText;
				XmlNodeList activities  = track.SelectNodes("Activity");
				foreach(XmlNode activity in activities)
				{
					string statusDescription = activity.SelectSingleNode("Status/StatusType/Description").InnerText;
					XmlNode nodeCity = activity.SelectSingleNode("ActivityLocation/Address/City");
					string city = (nodeCity == null ? "" : nodeCity.InnerText);
					XmlNode nodeState = activity.SelectSingleNode("ActivityLocation/Address/StateProvinceCode");
					string state = (nodeState == null ? "" : nodeState.InnerText);
					XmlNode nodeCountry = activity.SelectSingleNode("ActivityLocation/Address/CountryCode");
					string countryCode = (nodeCountry == null ? "" : nodeCountry.InnerText);
					string date = activity.SelectSingleNode("Date").InnerText;
					if(date != "")
						date = new System.DateTime(Int16.Parse(date.Substring(0, 4)), Int16.Parse(date.Substring(4, 2)), Int16.Parse(date.Substring(6, 2))).ToShortDateString();
					string time = activity.SelectSingleNode("Time").InnerText;
					if (time == "")
						time = new System.DateTime(1900, 1, 1, 11, 59, 59, 0).ToShortTimeString();
					else
						time = new System.DateTime(1900, 1, 1, Int16.Parse(time.Substring(0, 2)), Int16.Parse(time.Substring(2, 2)), Int16.Parse(time.Substring(4, 2))).ToShortTimeString();

					shipment.trackingActivities.Add(new TrackingActivity(trackingNumber, statusDescription, city, state, countryCode, date, time));
				}
			}
		}

		private void loadServiceCodes()
		{
			this._serviceCodes.Add("01", new AvailableService("Next Day Air", 1));
			this._serviceCodes.Add("02", new AvailableService("2nd Day Air", 2));
			this._serviceCodes.Add("03", new AvailableService("Ground", 4));
			this._serviceCodes.Add("07", new AvailableService("Worldwide Express", 8));
			this._serviceCodes.Add("08", new AvailableService("Worldwide Expedited", 16));
			this._serviceCodes.Add("11", new AvailableService("Standard", 32));
			this._serviceCodes.Add("12", new AvailableService("3-Day Select", 64));
			this._serviceCodes.Add("13", new AvailableService("Next Day Air Saver", 128));
			this._serviceCodes.Add("14", new AvailableService("Next Day Air Early AM", 256));
			this._serviceCodes.Add("54", new AvailableService("Worldwide Express Plus", 512));
			this._serviceCodes.Add("59", new AvailableService("2nd Day Air AM", 1024));
			this._serviceCodes.Add("65", new AvailableService("Express Saver", 2048));
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
