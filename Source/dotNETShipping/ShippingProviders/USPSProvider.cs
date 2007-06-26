using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Xml;

namespace dotNETShipping.ShippingProviders
{
	/// <summary>
	/// Summary description for USPSProvider.
	/// </summary>
	public class USPSProvider : AbstractShippingProvider
	{
		// TODO Add to .config
		// TODO	Change url to production server
		private const string url = "http://testing.shippingapis.com/ShippingAPITest.dll";

		private string _userID;
		private string _password;
        private Services _services;
		
		/// <summary>
		/// Creates an instance of the USPS API
		/// </summary>
		/// <param name="userID">Username</param>
		/// <param name="password">Password</param>
		public USPSProvider(string userID, string password)
		{
			this._name = "USPS";
			this._userID = userID;
			this._password = password;
            this._services = Services.Express;
		}

		/// <summary>
		/// Sends data to provider and gets the response
		/// </summary>
		public override void GetRates()
		{
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(USPSProvider.url);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			byte[] bytes = System.Text.Encoding.ASCII.GetBytes(this.buildRequestMessage());
			request.ContentLength = bytes.Length;
			Stream stream = request.GetRequestStream();
			stream.Write(bytes, 0, bytes.Length);
			stream.Close();
			// request.GetRequestStream().Write(bytes, 0, bytes.Length);
			System.Diagnostics.Debug.WriteLine("Request Sent!", "USPS");
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			this.parseResponseMessage(new StreamReader(response.GetResponseStream()).ReadToEnd());
			response.Close();
		}

		/// <summary>
		/// Used to build the message to be sent to the providers API
		/// </summary>
		/// <returns></returns>
		protected string buildRequestMessage()
		{
			System.Diagnostics.Debug.WriteLine("Building Request...", "USPS");
            //return this.buildRequestMessage(true);

			string request = "API=RateV2&XML=";
			
			XmlDocument xDoc = new XmlDocument();

            XmlNode xRoot = xDoc.AppendChild(xDoc.CreateElement("RateV2Request"));
			xRoot.Attributes.Append(xDoc.CreateAttribute("USERID")).Value = this._userID;
			xRoot.Attributes.Append(xDoc.CreateAttribute("PASSWORD")).Value = this._password;

			for (int i = 0; i < this._shipment.Packages.Count; i++)
			{
				XmlNode xPackage = xRoot.AppendChild(xDoc.CreateElement("Package"));
				xPackage.Attributes.Append(xDoc.CreateAttribute("ID")).Value = i.ToString();

                string service = this._shipment.Packages[i].Service == null ? _services.ToString() : this._shipment.Packages[i].Service;
				xPackage.AppendChild(xDoc.CreateElement("Service")).InnerText = service; //"EXPRESS";

                xPackage.AppendChild(xDoc.CreateElement("ZipOrigination")).InnerText = this._shipment.OriginAddress.PostalCode; //"20770"
                xPackage.AppendChild(xDoc.CreateElement("ZipDestination")).InnerText = this._shipment.DestinationAddress.PostalCode; //"20852"
				xPackage.AppendChild(xDoc.CreateElement("Pounds")).InnerText = this._shipment.Packages[i].Pounds.ToString(); //"10"				                
                xPackage.AppendChild(xDoc.CreateElement("Ounces")).InnerText = this._shipment.Packages[i].Ounces.ToString(); //"0"

                string container = this._shipment.Packages[i].Container == null ? Containers.None.ToString() : this._shipment.Packages[i].Container;
                xPackage.AppendChild(xDoc.CreateElement("Container")).InnerText = container; //"None"
				
                xPackage.AppendChild(xDoc.CreateElement("Size")).InnerText = GetSize(i); //"Regular"
				xPackage.AppendChild(xDoc.CreateElement("Machinable")).InnerText = this._shipment.Packages[i].Machinable.ToString(); //""
			}

			request += xDoc.OuterXml;
			System.Diagnostics.Debug.WriteLine(request);
			return request;
		}

        protected string buildRequestMessage(bool test)
        {
            string request = @"API=RateV2&XML=<RateV2Request USERID=""{0}"" PASSWORD=""{1}""><Package ID=""0""><Service>All</Service><ZipOrigination>10022</ZipOrigination><ZipDestination>20008</ZipDestination><Pounds>10</Pounds><Ounces>5</Ounces><Size>LARGE</Size><Machinable>TRUE</Machinable></Package></RateV2Request>";
            return string.Format(request, this._userID, this._password);
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="response"></param>
		private void parseResponseMessage(string response)
		{
			System.Diagnostics.Debug.WriteLine("USPS Response Received!");
			System.Diagnostics.Debug.WriteLine(response);
			XmlDocument xDoc = new XmlDocument();
			xDoc.LoadXml(response);
			XmlNodeList packages = xDoc.SelectNodes("/RateV2Response/Package");
			foreach(XmlNode package in packages)
			{
				string number = package.Attributes["ID"].InnerText;
				string name = "";
				string description = "";
				decimal totalCharges = 0.0m;
				try
				{
                    XmlNodeList postages = package.SelectNodes("Postage");
                    foreach (XmlNode postage in postages)
                    {
                        name = postage.SelectSingleNode("MailService").InnerText;
                        description = string.Empty;
                        totalCharges = Convert.ToDecimal(postage.SelectSingleNode("Rate").InnerText);				
                        this._shipment.rates.Add(new Rate("USPS", name, description, totalCharges, new DateTime(0)));
                    }					
				}
				catch (System.NullReferenceException)
				{
					XmlNode ErrorNode = package.SelectSingleNode("Error");
					description = ErrorNode.SelectSingleNode("Description").InnerText;
				}
			}
		}

		/// <summary>
		/// Returns the size of the package relative to the dimensions and weight
		/// </summary>
		/// <param name="PackageNumber"></param>
		/// <returns></returns>
		private string GetSize(int PackageNumber)
		{
			// TODO figure out the API's use of weight to determine oversized packages
			decimal pounds = this._shipment.Packages[PackageNumber].Pounds;
			decimal ounces = this._shipment.Packages[PackageNumber].Ounces;
			decimal length = this._shipment.Packages[PackageNumber].Length;
			decimal width = this._shipment.Packages[PackageNumber].Width;
			decimal height = this._shipment.Packages[PackageNumber].Height;

			decimal longest;
			decimal girth;
			if(length > width && length > height)
			{
				longest = length;
				girth = 2*(width + height);
			}
			else if(width > height)
			{
				longest = width;
				girth = 2*(length+height);
			}
			else
			{
				longest = height;
				girth = 2*(length+width);
			}

			string size = "";
			if (longest+girth <= 84)
			{
				size = Size.Regular.ToString();
			}
			else if(longest+girth > 84 && longest+girth <= 108)
			{
				size = Size.Large.ToString();
			}
			else if(longest+girth > 108 && longest+girth < 130)
			{
				size = Size.Oversize.ToString();
			}
			else
			{
			}

			return size;
		}

		/// <summary>
		/// Types of services provided
		/// </summary>
		public enum Services
		{
			/// <summary>
			/// Next day delivery to many locations, guaranteed or your money back.
			/// </summary>
			Express,
			/// <summary>
			/// Letters, envelopes, and small packages weighing 13 ounces or less.
			/// </summary>
			FirstClass, 
			/// <summary>
			/// Cost effective delivery in an average of 2-3 days.
			/// </summary>
			Priority, 
			/// <summary>
			/// 70lbs or less. Economical ground delivery service for mailing gifts and merchandise.
			/// </summary>
			Parcel, 
			/// <summary>
			/// 15lbs or less. For permanently bound advertising, promotional, directory or editorial material.
			/// </summary>
			BPM, 
			/// <summary>
			/// Library materials.
			/// </summary>
			Library,
			/// <summary>
			/// 70lbs or less. Send books, film, printed music, sound recordings and computer media.
			/// </summary>
			Media,
            /// <summary>
            /// Displays rates for all service types within one simple request.
            /// </summary>
            All
		}

		/// <summary>
		/// The types of containers USPS offers
		/// </summary>
		public enum Containers
		{
			/// <summary>
			/// For someone using their own package
			/// </summary>
			None,
			/// <summary>
			/// Express Mail Box, 12.25 x 15.5 x
			/// </summary>
			_ZeroDash_1093,
			/// <summary>
			/// Express Mail Tube, 36 x 6
			/// </summary>
			_ZeroDash_1094,
			/// <summary>
			/// Express Mail Cardboard Envelope, 12.5 x 9.5
			/// </summary>
			EP13A,
			/// <summary>
			/// Express Mail Tyvek Envelope, 12.5 x 15.5
			/// </summary>
			EP13C,
			/// <summary>
			/// Express Mail Flat Rate Envelope, 12.5 x 9.5
			/// </summary>
			EP13F,
			/// <summary>
			/// Priority Mail Box, 12.25 x 15.5 x 3
			/// </summary>
			_ZeroDash_1095,
			/// <summary>
			/// Priority Mail Video, 8.25 x 5.25 x 1.5
			/// </summary>
			_ZeroDash_1096,
			/// <summary>
			/// Priority Mail Box, 11.25 x 14 x 2.25
			/// </summary>
			_ZeroDash_1097,
			/// <summary>
			/// Priority Mail Tube, 6 x 38
			/// </summary>
			_ZeroDash_1098,
			/// <summary>
			/// Priority Mail Tyvek Envelope, 12.5 x 15.5
			/// </summary>
			EP14,
			/// <summary>
			/// Priority Mail Flat Rate Envelope, 12.5 x 9.5
			/// </summary>
			EP14F
		}

		/// <summary>
		/// The package sizes USPS allows
		/// </summary>
		public enum Size
		{
			/// <summary>
			/// Package length plus girth must equal 84 inches or less
			/// </summary>
			Regular,
			/// <summary>
			/// Parcels that weigh less than 15 pounds but measure more than 84 inches but less than 108 inches
			/// </summary>
			Large,
			/// <summary>
			/// Parcel Post packages that measure more than 108 inches but not more than 130 inches
			/// </summary>
			Oversize
		}
	}
}
