using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InProcessEventBus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EventBusTest.Controllers
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

        private readonly EventBus eventBus;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, EventBus eventBus)
        {
            _logger = logger;
            this.eventBus = eventBus;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var now = DateTime.Now;
            var TimeEventData = new TimeEventData() { EventTime = now, Time = now };
            eventBus.Publish(TimeEventData);

            var rng = new Random();
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
