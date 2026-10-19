using Microsoft.AspNetCore.SignalR;

namespace MqttWeatherApp
{
    public interface IWeatherForecastClient
    {
        Task ReceiveWeatherForecast(WeatherForecast forecast);
    }

    public class WeatherForecastHub : Hub<IWeatherForecastClient>
    {
    }
}
