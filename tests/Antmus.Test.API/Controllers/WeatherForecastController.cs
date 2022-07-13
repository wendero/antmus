using Microsoft.AspNetCore.Mvc;

namespace Antmus.Test.API.Controllers
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
            var temperatures = new int[] { -16, 31, -18, 3, 22, 7 };
            var baseDate = new DateTime(2022, 02, 01);

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = baseDate.AddDays(index),
                TemperatureC = temperatures[index],
                Summary = Summaries[index]
            })
            .ToArray();
        }
    }
}