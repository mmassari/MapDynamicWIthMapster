using Mapster;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using System;

namespace MapDynamicWithMapster
{
	class Program
	{
		public static MockHttpMessageHandler mockHttp = new MockHttpMessageHandler();
		static void Main(string[] args)
		{
			SetupMockServer();
			
			Console.WriteLine("\nGetting customer data...");
			var customerData = GetData<Customer>("http://contoso.come/customer/157");
			if (customerData.IsError)
				Console.WriteLine($"ERROR! {customerData.ErrorMessage}");
			else
				Console.WriteLine($"The response is OK. Customer name is {customerData.data.name}");

			Console.WriteLine("\nGetting customer data with error...");
			var customerError = GetData<Customer>("http://contoso.come/customers");
			if (customerError.IsError)
				Console.WriteLine($"ERROR! {customerError.ErrorMessage}");
			else
				Console.WriteLine($"The response is OK. Customer name is {customerError.data.name}");

			Console.WriteLine("\nGetting supplier data...");
			var supplierData = GetData<Supplier>("http://contoso.come/supplier/1885");
			if (supplierData.IsError)
				Console.WriteLine($"ERROR! {supplierData.ErrorMessage}");
			else
				Console.WriteLine($"The response is OK. Customer name is {supplierData.data.company}");

			Console.WriteLine("\nGetting version string data...");
			var versionData = GetData<string>("http://contoso.come/version/");
			if (versionData.IsError)
				Console.WriteLine($"ERROR! {versionData.ErrorMessage}");
			else
				Console.WriteLine($"The response is OK. The version is {versionData.data}");

			Console.WriteLine("\nGetting supplier data (but using customer endpoint)");
			var wrongData = GetData<Supplier>("http://contoso.come/customer/76");
			if (wrongData.IsError)
				Console.WriteLine($"ERROR! {wrongData.ErrorMessage}");
			else
				Console.WriteLine($"The response is OK. Customer name is {wrongData.data.company}");

			var customerJsonData = ParseJson<Customer>("{\"type\":\"send\", \"data\": {\"id\":1, \"name\": \"John Ross\"}}");
		}

		/// <summary>
		/// Helper utility to get the type from the url in a single call
		/// </summary>
		private static Response<T> GetData<T>(string url) where T: class => ParseJson<T>(GetJsonFromUrl(url));
		

		/// <summary>
		/// Call the mock Http server and get back a json response
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		private static string GetJsonFromUrl(string url)
		{
			var response = mockHttp.ToHttpClient().GetAsync(url).Result;
			if (!response.IsSuccessStatusCode)
				throw new ApplicationException($"The request is failed with error {response.StatusCode}");

			return response.Content.ReadAsStringAsync().Result;
		}
		/// <summary>
		/// Parse Json string and deserialize in a Respose<T> object managing also the eventual error
		/// </summary>
		private static Response<T> ParseJson<T>(string json) where T : class
		{
			var dynamicResponse = JsonConvert.DeserializeObject<Response<dynamic>>(json);
			var output = dynamicResponse.Adapt<Response<T>>();

			if (dynamicResponse.type == "error")
			{
				output.Error = ((object)dynamicResponse.data).Adapt<Error>();
				output.data = null;
			}
			return output;
		}
		/// <summary>
		/// Setup the Mock HTTP server to fake the api calls
		/// </summary>
		private static void SetupMockServer()
		{
			Console.WriteLine("Setting up a mock web server...");
			mockHttp.When("http://contoso.come/customer/*")
					  .Respond("application/json", JsonConvert.SerializeObject(new Response<Customer>
					  {
						  type = "data",
						  data = new Customer { customerId = 1, name = "Mike Ross" }
					  }));
			mockHttp.When("http://contoso.come/supplier/*")
					  .Respond("application/json", JsonConvert.SerializeObject(new Response<Supplier>
					  {
						  type = "data",
						  data = new Supplier { supplierId = 1, company = "Microsoft" }
					  }));
			mockHttp.When("http://contoso.come/customers")
					  .Respond("application/json", JsonConvert.SerializeObject(new Response<Error>
					  {
						  type = "error",
						  data = new Error { id = 99, description = "Error data type" }
					  }));
			mockHttp.When("http://contoso.come/version/")
					  .Respond("application/json", JsonConvert.SerializeObject(new Response<string>
					  {
						  type = "data",
						  data = "v 2.15.144"
					  }));
		}


	}

	public class Response<T> where T : class
	{
		public string type { get; set; }
		public T data { get; set; } = null;
		public Error Error { get; set; } = null;
		public bool IsError => Error != null;
		public string ErrorMessage => IsError ? $"An error occurred. Error code {Error.id} - {Error.description}" : "";
	}
	public class Customer
	{
		public int customerId { get; set; }
		public string name { get; set; }
	}
	public class Supplier
	{
		public int supplierId { get; set; }
		public string company { get; set; }
	}
	public class Error
	{
		public int id { get; set; }
		public string description { get; set; }
	}
}
