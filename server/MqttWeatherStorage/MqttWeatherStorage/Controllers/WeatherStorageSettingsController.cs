using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace MqttWeatherStorage.Controllers;

[ApiController]
[Route("storage-settings")]
public class WeatherStorageSettingsController : ControllerBase
{
    [HttpGet]
    public WeatherStorageSettings Get()
    {
        return WeatherStorageSettingsStore.Get();
    }

    [HttpPut]
    public ActionResult<WeatherStorageSettings> Update([FromBody] UpdateWeatherStorageSettingsRequest request)
    {
        var settings = new WeatherStorageSettings(request.Enabled, request.StoreTime);
        WeatherStorageSettingsStore.Update(settings);
        return Ok(settings);
    }
}

public sealed class UpdateWeatherStorageSettingsRequest
{
    public bool Enabled { get; init; }

    [Range(1, int.MaxValue)]
    public int StoreTime { get; init; }
}
