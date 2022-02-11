# Map Dynamic data with Mapster
A sample c# application to work with dynamic data from API endpoint.

## The problem

I have to manage a lot of API calls to a service and all the response message have a common structure except the "data" field which varies according to the endpoint called and whether the call was successful or not.

These are some response messages:

```json
{
    "type": "send",
    "datetime": "2022-02-21",
    "correlation_id": "dc659b16-0781-4e32-ae0d-fbe737ff3215",
    "data": {
	    "id": 22,
	    "description": "blue t-shirt with stripes",
	    "category": "t-shirt",
	    "size": "XL"
    }
}
{
    "type": "send",
    "datetime": "2022-02-18",
    "correlation_id": "dc659b16-0781-4e32-ae0d-fbe737ff3215",
    "data": "version 15.2"
}
{
    "type": "send",
    "datetime": "2022-02-18",
    "correlation_id": "dc659b16-0781-4e32-ae0d-fbe737ff3215",
    "data": {
        "cart_id": 22,
        "items": [
            {"id": 5, "description": "product 2"},
            {"id": 12, "description": "product 3"},
        ] 
    }
}
```
The data field is totally variable, it can be a simple one-level structure, a complex multi-level structure or an array, and can even be a simple string or null.

Clearly the response messages are known and depends on the called endpoint so I know what to expect when I make the call but if the server throw an error the response is like this:
```json
{
   "type": "error",
   "datetime": "2022-02-21",
   "correlation_id": "dc659b16-0781-4e32-ae0d-fbe737ff3215",
   "data": {
	"id": 1522,
	"description": "product code not found",
   }
}

```
## The solution
Looking for an elegant and concise solution to manage all cases with a single method, I was able to find a solution:
These are my models:

```csharp
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
```

And this is my function that manage all the deserializations:

```csharp
private static Response<T> GetData<T>(string json) where T : class
{
    //Deserialize the json using dynamic as T so I can receive any kind of data structure
    var resp = JsonConvert.DeserializeObject<Response<dynamic>>(json);

    //Adapting the dynamic to the requested T type
    var ret = resp.Adapt<Response<T>>();

	if (resp.type == "error")
	{
		//Adapt the dynamic to Error property
		ret.Error = ((object)resp.data).Adapt<Error>();
		ret.data = null;
	}
	return ret;
}
```

So I call my function in this way:

```csharp
var customerData = GetData<Customer>("{\"type\":\"send\", \"data\": {\"id\":1, \"name\": \"John Ross\"}}");
if (customerData.IsError)
	Console.WriteLine($"ERROR! {customerData.ErrorMessage}");
else
	Console.WriteLine($"The response is OK. Customer name is {customerData.data.name}");
```
## Open Issues
- Looking for a way to catch errors in case i try to deserialize a json that doesn't fit with T. Indeed Mapster Adapt doesn't fail if I try to fit the wrong json to type T but fill only the properties that look similar.
- With generics I can accept all kind of data but not value types so I can't work with most of simple types like int, decimal o DateTime.
