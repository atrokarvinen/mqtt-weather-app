using MqttWeatherStorage.Controllers;
using System.Collections.Concurrent;

namespace MqttWeatherStorage;

public class MqttWeatherStoreQueue(ILogger<MqttWeatherStoreQueue> logger) : BackgroundService
{
    private static readonly ConcurrentQueue<WeatherForecast> _weatherForecastQueue = [];

    public async Task HandleMessage(WeatherForecast forecast)
    {
        logger.LogInformation("Handling weather forecast {Id} received from MQTT.", forecast.Id);

        await WeatherStorageController.StoreForecast(forecast, WeatherStorageSettingsStore.Get().StoreTime);

        logger.LogInformation("Finished handling weather forecast {Id} received from MQTT. Queue length: {QueueLength}.", forecast.Id, GetQueueLength());
    }

    public static void Enqueue(WeatherForecast forecast)
    {
        _weatherForecastQueue.Enqueue(forecast);
    }

    public static bool TryDequeue(out WeatherForecast? forecast)
    {
        return _weatherForecastQueue.TryDequeue(out forecast);
    }

    public static int GetQueueLength()
    {
        return _weatherForecastQueue.Count;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (TryDequeue(out var forecast))
            {
                if (forecast == null)
                {
                    continue;
                }
                await HandleMessage(forecast);
            }
            else
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}