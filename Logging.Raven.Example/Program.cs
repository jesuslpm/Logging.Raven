using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;

namespace Logging.Raven.Example
{
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
                    //logging.AddRaven(options =>
                    //{
                    //    options.Database = "test";
                    //    options.Expiration = TimeSpan.FromDays(2);
                    //});
                    logging.AddRaven();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
