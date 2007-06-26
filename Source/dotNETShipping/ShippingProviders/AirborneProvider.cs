using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Xml;

namespace dotNETShipping.ShippingProviders
{
	/// <summary>
	/// Provides rates from Airborne Express.
	/// </summary>
	public class AirborneProvider : AbstractShippingProvider
	{
		public enum AvailableServices
		{
			NextBusinessDay = 1,
			NextBusinessDayAM = 2,
			NextBusinessDayPM = 4,
			SecondBusinessDay = 8,
			Ground = 16,
			All = 31
		}

		private const string url = "https://eCommerce.airborne.com/ApiLandingTest.asp";
		private const int defaultTimeout = 15;

		private string _userID;
		private string _password;
		private string _accountNumber;
		private string _shippingKey;
		private int _timeout;
		private Hashtable _serviceCodes = new Hashtable(5);
		private AvailableServices _services = AvailableServices.All;

		public AirborneProvider(string userID, string password, string accountNumber, string shippingKey) : this(userID, password, accountNumber, shippingKey, AirborneProvider.defaultTimeout)
		{}

		public AirborneProvider(string userID, string password, string accountNumber, string shippingKey, int timeout)
		{
			this._name = "Airborne";
			this._userID = userID;
			this._password = password;
			this._accountNumber = accountNumber;
			this._shippingKey = shippingKey;
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
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(AirborneProvider.url);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.Timeout = this._timeout * 1000;
			byte[] bytes = System.Text.Encoding.ASCII.GetBytes(this.buildRequestMessage());
			request.ContentLength = bytes.Length;
			Stream stream = request.GetRequestStream();
			stream.Write(bytes, 0, bytes.Length);
			stream.Close();
			System.Diagnostics.Debug.WriteLine("Request Sent!", "Airborne");
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			this.parseResponseMessage(new StreamReader(response.GetResponseStream()).ReadToEnd());
			response.Close();
		}

		private string buildRequestMessage()
		{
			System.Diagnostics.Debug.WriteLine("Building Request...", "Airborne");
			string request = "";
			request += "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n";

			XmlNode baseShipment = null;

			XmlDocument xDoc = new XmlDocument();
			XmlNode eCommerce = xDoc.AppendChild(xDoc.CreateElement("eCommerce"));
			eCommerce.Attributes.Append(xDoc.CreateAttribute("action")).Value = "Request";
			eCommerce.Attributes.Append(xDoc.CreateAttribute("version")).Value = "1.1";
			XmlNode xRequestor = eCommerce.AppendChild(xDoc.CreateElement("Requestor"));
			xRequestor.AppendChild(xDoc.CreateElement("ID")).InnerText = this._userID;
			xRequestor.AppendChild(xDoc.CreateElement("Password")).InnerText = this._password;

			XmlNode xShipment = eCommerce.AppendChild(xDoc.CreateElement("Shipment"));
			xShipment.Attributes.Append(xDoc.CreateAttribute("action")).Value = "RateEstimate";
			xShipment.Attributes.Append(xDoc.CreateAttribute("version")).Value = "1.0";
			XmlNode xCredentials = xShipment.AppendChild(xDoc.CreateElement("ShippingCredentials"));
			xCredentials.AppendChild(xDoc.CreateElement("ShippingKey")).InnerText = this._shippingKey;
			xCredentials.AppendChild(xDoc.CreateElement("AccountNbr")).InnerText = this._accountNumber;
			XmlNode xShipmentDetail = xShipment.AppendChild(xDoc.CreateElement("ShipmentDetail"));
			xShipmentDetail.AppendChild(xDoc.CreateElement("ShipDate")).InnerText = System.DateTime.Now.AddDays(2).ToString("yyyy-MM-dd");
			XmlNode xShipmentType = xShipmentDetail.AppendChild(xDoc.CreateElement("ShipmentType"));
			xShipmentType.AppendChild(xDoc.CreateElement("Code")).InnerText = "P";
			xShipmentDetail.AppendChild(xDoc.CreateElement("Weight")).InnerText = this._shipment.Packages[0].Weight.ToString();
			XmlNode xDimensions = xShipmentDetail.AppendChild(xDoc.CreateElement("Dimensions"));
			xDimensions.AppendChild(xDoc.CreateElement("Length")).InnerText = this._shipment.Packages[0].Length.ToString();
			xDimensions.AppendChild(xDoc.CreateElement("Width")).InnerText = this._shipment.Packages[0].Width.ToString();
			xDimensions.AppendChild(xDoc.CreateElement("Height")).InnerText = this._shipment.Packages[0].Height.ToString();
			XmlNode xProtection = xShipmentDetail.AppendChild(xDoc.CreateElement("AdditionalProtection"));
			xProtection.AppendChild(xDoc.CreateElement("Code")).InnerText = "AP";
			xProtection.AppendChild(xDoc.CreateElement("Value")).InnerText = this._shipment.Packages[0].InsuredValue.ToString();
			XmlNode xBilling = xShipment.AppendChild(xDoc.CreateElement("Billing"));
			XmlNode xBillingParty = xBilling.AppendChild(xDoc.CreateElement("Party"));
			xBillingParty.AppendChild(xDoc.CreateElement("Code")).InnerText = "S";
			XmlNode xReceiver = xShipment.AppendChild(xDoc.CreateElement("Receiver"));
			XmlNode xReceiverAddress = xReceiver.AppendChild(xDoc.CreateElement("Address"));
			xReceiverAddress.AppendChild(xDoc.CreateElement("State")).InnerText = this._shipment.DestinationAddress.State;
			xReceiverAddress.AppendChild(xDoc.CreateElement("PostalCode")).InnerText = this._shipment.DestinationAddress.PostalCode;
			xReceiverAddress.AppendChild(xDoc.CreateElement("Country")).InnerText = this._shipment.DestinationAddress.CountryCode;
			baseShipment = xShipment.Clone();

			XmlNode xService = null;

			if((this._services & AvailableServices.NextBusinessDay) == AvailableServices.NextBusinessDay)
			{
				// Express service
				xService = xShipmentDetail.AppendChild(xDoc.CreateElement("Service"));
				xService.AppendChild(xDoc.CreateElement("Code")).InnerText = "E";
			}

			if((this._services & AvailableServices.NextBusinessDayAM) == AvailableServices.NextBusinessDayAM)
			{
				// Express 10:30 AM service
				xShipment = baseShipment.Clone();
				xShipmentDetail = xShipment.SelectSingleNode("ShipmentDetail");
				xService = xShipmentDetail.AppendChild(xDoc.CreateElement("Service"));
				xService.AppendChild(xDoc.CreateElement("Code")).InnerText = "E";
				XmlNode xSpecialServices = xShipmentDetail.AppendChild(xDoc.CreateElement("SpecialServices"));
				XmlNode xSpecialService = xSpecialServices.AppendChild(xDoc.CreateElement("SpecialService"));
				xSpecialService.AppendChild(xDoc.CreateElement("Code")).InnerText = "1030";
				xShipment.AppendChild(xDoc.CreateElement("TransactionTrace")).InnerText = "1030";
				eCommerce.AppendChild(xShipment);
			}

			if((this._services & AvailableServices.NextBusinessDayPM) == AvailableServices.NextBusinessDayPM)
			{
				// Next Afternoon service
				xShipment = baseShipment.Clone();
				xShipmentDetail = xShipment.SelectSingleNode("ShipmentDetail");
				xService = xShipmentDetail.AppendChild(xDoc.CreateElement("Service"));
				xService.AppendChild(xDoc.CreateElement("Code")).InnerText = "N";
				eCommerce.AppendChild(xShipment);
			}

			if((this._services & AvailableServices.SecondBusinessDay) == AvailableServices.SecondBusinessDay)
			{
				// Second Day service
				xShipment = baseShipment.Clone();
				xShipmentDetail = xShipment.SelectSingleNode("ShipmentDetail");
				xService = xShipmentDetail.AppendChild(xDoc.CreateElement("Service"));
				xService.AppendChild(xDoc.CreateElement("Code")).InnerText = "S";
				eCommerce.AppendChild(xShipment);
			}

			if((this._services & AvailableServices.Ground) == AvailableServices.Ground)
			{
				// Ground service
				xShipment = baseShipment.Clone();
				xShipmentDetail = xShipment.SelectSingleNode("ShipmentDetail");
				xService = xShipmentDetail.AppendChild(xDoc.CreateElement("Service"));
				xService.AppendChild(xDoc.CreateElement("Code")).InnerText = "G";
				eCommerce.AppendChild(xShipment);
			}

			request += xDoc.OuterXml;
			//System.Diagnostics.Debug.WriteLine(request);
			return request;
		}

		private void parseResponseMessage(string response)
		{
			System.Diagnostics.Debug.WriteLine(response);
			XmlDocument xDoc = new XmlDocument();
			xDoc.LoadXml(response);
			XmlNodeList shipments = xDoc.SelectNodes("/eCommerce/Shipment");
			if(shipments != null)
			{
				foreach(XmlNode shipment in shipments)
				{
					if(shipment.SelectNodes("Faults").Count == 0)
					{
						XmlNodeList estimate = shipment.SelectNodes("EstimateDetail");
						string serviceCode = shipment.SelectSingleNode("EstimateDetail/Service/Code").InnerText;
						string serviceDesc = shipment.SelectSingleNode("EstimateDetail/ServiceLevelCommitment/Desc").InnerText;

						XmlNode transTrace = shipment.SelectSingleNode("TransactionTrace");
						if(transTrace != null)
							serviceCode += transTrace.InnerText;
						if(this._serviceCodes.ContainsKey(serviceCode))
							serviceDesc = this._serviceCodes[serviceCode].ToString();
						decimal totalCharges = decimal.Parse(shipment.SelectSingleNode("EstimateDetail/RateEstimate/TotalChargeEstimate").InnerText);
						this._shipment.rates.Add(new Rate(this.Name, serviceCode, serviceDesc, totalCharges, this.getDeliveryDateTime(serviceCode)));
					}
				}
			}
		}

		private void loadServiceCodes()
		{
			this._serviceCodes.Add("E", "Next Business Day");
			this._serviceCodes.Add("E1030", "Next Business Day AM");
			this._serviceCodes.Add("N", "Next Business Day PM");
			this._serviceCodes.Add("S", "Second Business Day");
			this._serviceCodes.Add("G", "Ground (est. 3 days)");
		}

		public System.DateTime getDeliveryDateTime(string serviceCode)
		{
			System.DateTime pickupDate = System.DateTime.Today.AddDays(2);

			System.DateTime result = pickupDate;
			int daysToAdd = 0;
			switch(serviceCode)
			{
				case "E":
				case "E1030":
				case "N":
					switch(pickupDate.DayOfWeek)
					{
						case DayOfWeek.Friday:
							daysToAdd = 3;
							break;
						case DayOfWeek.Saturday:
							daysToAdd = 2;
							break;
						default:
							daysToAdd = 1;
							break;
					}
					break;
				case "S":
					switch(pickupDate.DayOfWeek)
					{
						case DayOfWeek.Thursday:
						case DayOfWeek.Friday:
							daysToAdd = 4;
							break;
						case DayOfWeek.Saturday:
							daysToAdd = 3;
							break;
						default:
							daysToAdd = 2;
							break;
					}
					break;
				case "G":
					result = System.DateTime.Parse("1/1/1900");
					break;
			}
			result = result.AddDays(daysToAdd);

			switch(serviceCode)
			{
				case "N":
					result = result.AddHours(15);
					break;
				case "E":
					result = result.AddHours(12);
					break;
				case "E1030":
					result = result.AddHours(10.5);
					break;
				case "S":
					result = result.AddHours(17);
					break;
			}
			return result;
		}
	}
}
