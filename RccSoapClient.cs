using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;

namespace _2017Launcher
{
	internal sealed class RccLuaArgument
	{
		public string Type { get; private set; }
		public string Value { get; private set; }

		public RccLuaArgument(string type, string value)
		{
			Type = type ?? "LUA_TSTRING";
			Value = value ?? string.Empty;
		}
	}

	/// <summary>
	/// Small RCC SOAP client used for the live appearance update and the
	/// launcher's existing local OpenJob/Execute flows.
	/// </summary>
	internal static class RccSoapClient
	{
		private const string SoapNamespace = "http://roblox.com/";
		private static readonly HttpClient Client = new HttpClient(new HttpClientHandler { UseProxy = false })
		{
			Timeout = TimeSpan.FromSeconds(15)
		};

		public static string Execute(string host, int port, string jobId, string scriptName, string script)
		{
			XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
			XNamespace rob = SoapNamespace;
			XElement body = new XElement(rob + "Execute",
				new XElement(rob + "jobID", jobId ?? "Test"),
				BuildScript(rob, scriptName, script, null));
			return Send(host, port, "Execute", Envelope(soap, rob, body));
		}

		public static string OpenJob(string host, int port, string jobId, double expirationSeconds, int category, double cores, string scriptName, string script, IEnumerable<RccLuaArgument> arguments)
		{
			XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
			XNamespace rob = SoapNamespace;
			XElement body = new XElement(rob + "OpenJob",
				new XElement(rob + "job",
					new XElement(rob + "id", jobId ?? "Test"),
					new XElement(rob + "expirationInSeconds", expirationSeconds.ToString(CultureInfo.InvariantCulture)),
					new XElement(rob + "category", category.ToString(CultureInfo.InvariantCulture)),
					new XElement(rob + "cores", cores.ToString(CultureInfo.InvariantCulture))),
				BuildScript(rob, scriptName, script, arguments));
			return Send(host, port, "OpenJob", Envelope(soap, rob, body));
		}

		private static XElement BuildScript(XNamespace rob, string name, string script, IEnumerable<RccLuaArgument> arguments)
		{
			XElement args = new XElement(rob + "arguments");
			foreach (RccLuaArgument argument in arguments ?? Enumerable.Empty<RccLuaArgument>())
			{
				args.Add(new XElement(rob + "LuaValue",
					new XElement(rob + "type", argument.Type),
					new XElement(rob + "value", argument.Value)));
			}

			return new XElement(rob + "script",
				new XElement(rob + "name", name ?? string.Empty),
				new XElement(rob + "script", new XCData((script ?? string.Empty).Replace("]]>", "]]]]><![CDATA[>"))),
				args);
		}

		private static XElement Envelope(XNamespace soap, XNamespace rob, XElement body)
		{
			return new XElement(soap + "Envelope",
				new XAttribute(XNamespace.Xmlns + "soapenv", soap.NamespaceName),
				new XAttribute(XNamespace.Xmlns + "rob", rob.NamespaceName),
				new XElement(soap + "Header"),
				new XElement(soap + "Body", body));
		}

		private static string Send(string host, int port, string action, XElement envelope)
		{
			using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "http://" + host + ":" + port.ToString(CultureInfo.InvariantCulture) + "/"))
			{
				request.Headers.TryAddWithoutValidation("SOAPAction", SoapNamespace + action);
				request.Content = new StringContent(envelope.ToString(SaveOptions.DisableFormatting), new UTF8Encoding(false), "text/xml");
				using (HttpResponseMessage response = Client.SendAsync(request).GetAwaiter().GetResult())
				{
					string result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
					if (!response.IsSuccessStatusCode)
					{
						throw new InvalidOperationException("RCC returned HTTP " + (int)response.StatusCode + ": " + result);
					}

					XDocument document = XDocument.Parse(result);
					XElement fault = document.Descendants().FirstOrDefault(element => element.Name.LocalName == "Fault");
					if (fault != null)
					{
						throw new InvalidOperationException("RCC SOAP fault: " + fault.Value.Trim());
					}

					return result;
				}
			}
		}
	}
}
