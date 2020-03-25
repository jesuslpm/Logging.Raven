# Logging.Raven
RavenDB structured logging provider for .net core 3.1. Store your logs in a RavenDB database and query them easily.

To add Raven logging provider to your asp.net core application you need to add it to your program.cs. For example:
```
public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    static IDocumentStore CreateDocumentStore(IServiceProvider serviceProvider)
    {
        var store = new DocumentStore
        {
            Database = "test",
            Urls = new string[] { "http://localhost:8080" }
        };
        store.Initialize();
        return store;
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.Services.AddSingleton(CreateDocumentStore);
                logging.AddRaven();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
```

You can configure it like any other standard provider in appsettings.json. For example:
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    },
    "Raven": {
      "LogLevel": {
        "Default": "Warning",
        "Microsoft": "Error"
      },
      "Database": "test",
      "Expiration": "3",
      "IncludeScopes": true
    }
  },
  "AllowedHosts": "*"
}
```
Accoding to the above appsetings file, Raven log entries will be stored in test database, they will expire after 3 days and they will include scopes. Only `Critical` and `Error` log levels will be stored for category names starting with `Microsoft`, while other categories will store `Warning`, `Error` and `Critical` logs


A structured log like the following:
```
logger.LogInformation("Weather forecast called using {Service}", "International Weather Forecast Service");
```
Produces a RavenDB document like the following. Please notice `State` with `Service` and `{OriginalFormat}` along with the traditional `Text` message. It also inludes a lot of information about the log, such as `Level`, `TimeStamp`, `Category` and so on. `Scopes` is also interesting, it provides you information about the http request, the activity and action.
```
{
    "Category": "Logging.Raven.Example.Controllers.WeatherForecastController",
    "Level": "Information",
    "Text": "Weather forecast called using International Weather Forecast Service",
    "Exception": null,
    "IdentityName": null,
    "TimeStamp": "2020-03-25T17:07:26.4985347Z",
    "TraceIdentifier": "80000019-0007-ff00-b63f-84710c7967bb",
    "State": {
        "Service": "International Weather Forecast Service",
        "{OriginalFormat}": "Weather forecast called using {Service}"
    },
    "Scopes": [
        {
            "RequestId": "80000019-0007-ff00-b63f-84710c7967bb",
            "RequestPath": "/weatherforecast",
            "SpanId": "|8621a801-478d3d9ec73c7280.",
            "TraceId": "8621a801-478d3d9ec73c7280",
            "ParentId": ""
        },
        {
            "ActionId": "f3ebbde4-583f-414c-b103-203c72bdf320",
            "ActionName": "Logging.Raven.Example.Controllers.WeatherForecastController.Get (Logging.Raven.Example)"
        }
    ],
    "UserName": "jesus.lopez",
    "HostName": "DELL-JLM",
    "EventId": {
        "Id": 0,
        "Name": null
    },
    "@metadata": {
        "@collection": "RavenLogEntries",
        "Raven-Clr-Type": "Logging.Raven.RavenLogEntry, Logging.Raven",
        "@expires": "2020-03-28T17:07:28.9148757Z"
    }
}
```

Information about errors is also included when logging exceptions like this:
```
try
{
    throw new InvalidOperationException("Invalid operation");
}
catch (Exception ex)
{
    this._logger.LogError(ex, "Something went wrong");
}
```
In this case you get a document like the following:
```
{
    "Category": "Logging.Raven.Example.Controllers.WeatherForecastController",
    "Level": "Error",
    "Text": "Something went wrong",
    "Exception": {
        "$type": "System.InvalidOperationException, System.Private.CoreLib",
        "ClassName": "System.InvalidOperationException",
        "Message": "Invalid operation",
        "Data": null,
        "InnerException": null,
        "HelpURL": null,
        "StackTraceString": "   at Logging.Raven.Example.Controllers.WeatherForecastController.Get() in C:\\projects\\Logging.Raven\\Logging.Raven.Example\\Controllers\\WeatherForecastController.cs:line 32",
        "RemoteStackTraceString": null,
        "RemoteStackIndex": 0,
        "ExceptionMethod": null,
        "HResult": -2146233079,
        "Source": "Logging.Raven.Example",
        "WatsonBuckets": null
    },
    "IdentityName": null,
    "TimeStamp": "2020-03-25T17:07:26.6907338Z",
    "TraceIdentifier": "80000019-0007-ff00-b63f-84710c7967bb",
    "State": {
        "{OriginalFormat}": "Something went wrong"
    },
    "Scopes": [
        {
            "RequestId": "80000019-0007-ff00-b63f-84710c7967bb",
            "RequestPath": "/weatherforecast",
            "SpanId": "|8621a801-478d3d9ec73c7280.",
            "TraceId": "8621a801-478d3d9ec73c7280",
            "ParentId": ""
        },
        {
            "ActionId": "f3ebbde4-583f-414c-b103-203c72bdf320",
            "ActionName": "Logging.Raven.Example.Controllers.WeatherForecastController.Get (Logging.Raven.Example)"
        }
    ],
    "UserName": "jesus.lopez",
    "HostName": "DELL-JLM",
    "EventId": {
        "Id": 0,
        "Name": null
    },
    "@metadata": {
        "@collection": "RavenLogEntries",
        "Raven-Clr-Type": "Logging.Raven.RavenLogEntry, Logging.Raven",
        "@expires": "2020-03-28T17:07:28.9162152Z"
    }
}
```


