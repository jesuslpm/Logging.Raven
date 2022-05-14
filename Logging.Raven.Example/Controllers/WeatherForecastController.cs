using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Logging.Raven.Example.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            this._logger.LogInformation("Weather forecast called using {Service}", "International Weather Forecast Service");
            try
            {
                throw new InvalidOperationException("Invalid operation");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Something went wrong");
            }

           
            var rng = new Random();

            for (int i = 0; i < 10200; i++)
            {
                this._logger.LogInformation(new EventId(50, "MyEvent"), "Structured logging with object parameter: {Parameter}", new { Id = 1, Name = "Jesús" });
            }
            
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
