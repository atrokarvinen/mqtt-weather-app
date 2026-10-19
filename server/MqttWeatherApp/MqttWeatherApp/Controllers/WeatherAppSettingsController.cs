using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace MqttWeatherApp.Controllers
{
    [ApiController]
    [Route("app-settings")]
    public class WeatherAppSettingsController : ControllerBase
    {
        [HttpGet]
        public WeatherAppSettings Get()
        {
            return WeatherAppSettingsStore.Get();
        }

        [HttpPut]
        public ActionResult<WeatherAppSettings> Update([FromBody] UpdateWeatherAppSettingsRequest request)
        {
            var settings = new WeatherAppSettings(request.Enabled, request.SendInterval);
            WeatherAppSettingsStore.Update(settings);
            return Ok(settings);
        }
    }

    public sealed class UpdateWeatherAppSettingsRequest
    {
        public bool Enabled { get; init; }

        [Range(1, int.MaxValue)]
        public int SendInterval { get; init; }
    }
}
