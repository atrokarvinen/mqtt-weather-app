using Microsoft.AspNetCore.Mvc;

namespace MqttWeatherStorage.Controllers;

[ApiController]
[Route("weather-storage")]
public class WeatherStorageController : ControllerBase
{
    private static readonly List<WeatherForecast> StoredForecasts = [];
    private static readonly Lock StoredForecastsLock = new();

    [HttpPost]
    public IEnumerable<WeatherForecast> AddWeatherForecasts([FromBody] IEnumerable<WeatherForecast> forecasts)
    {
        lock (StoredForecastsLock)
        {
            StoredForecasts.AddRange(forecasts);
            return StoredForecasts.ToArray();
        }
    }

    [HttpGet]
    public IEnumerable<WeatherForecast> GetStoredForecasts()
    {
        lock (StoredForecastsLock)
        {
            return StoredForecasts.ToArray();
        }
    }

    [HttpDelete]
    public IActionResult ClearStoredForecasts()
    {
        lock (StoredForecastsLock)
        {
            StoredForecasts.Clear();
        }
        return NoContent();
    }

    [HttpPut]
    public IEnumerable<WeatherForecast> UpdateStoredForecasts([FromBody] IEnumerable<WeatherForecast> forecasts)
    {
        lock (StoredForecastsLock)
        {
            StoredForecasts.Clear();
            StoredForecasts.AddRange(forecasts);
            return StoredForecasts.ToArray();
        }
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteStoredForecast(Guid id)
    {
        lock (StoredForecastsLock)
        {
            var forecast = StoredForecasts.FirstOrDefault(f => f.Id == id);
            if (forecast == null)
            {
                return NotFound();
            }
            StoredForecasts.Remove(forecast);
        }
        return NoContent();
    }

    internal static async Task StoreForecast(WeatherForecast forecast, int storeTime)
    {
        await Task.Delay(storeTime);
        lock (StoredForecastsLock)
        {
            if (StoredForecasts.All(storedForecast => storedForecast.Id != forecast.Id))
            {
                StoredForecasts.Add(forecast);
            }
        }
    }
}
