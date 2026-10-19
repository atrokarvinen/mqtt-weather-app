using Microsoft.AspNetCore.Mvc;

namespace MqttWeatherApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5)
                .Select(index => WeatherForecastFactory.Create(DateOnly.FromDateTime(DateTime.Now.AddDays(index))))
                .ToArray();
        }
    }
}
