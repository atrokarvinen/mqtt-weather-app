using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using System.Buffers;
using System.Text.Json;

namespace MqttWeatherApp
{
    public class WeatherForecastNotifier : BackgroundService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly MqttOptions _mqttOptions;
        private readonly IHubContext<WeatherForecastHub, IWeatherForecastClient> _hubContext;
        private readonly ILogger<WeatherForecastNotifier> _logger;

        public WeatherForecastNotifier(
            IOptions<MqttOptions> mqttOptions,
            IHubContext<WeatherForecastHub, IWeatherForecastClient> hubContext,
            ILogger<WeatherForecastNotifier> logger)
        {
            _mqttOptions = mqttOptions.Value;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new MqttClientFactory();
            using var mqttClient = factory.CreateMqttClient();
            mqttClient.ApplicationMessageReceivedAsync += HandleMessageAsync;

            var clientOptions = MqttOptionsFactory.CreateClientOptions(
                _mqttOptions,
                $"weather-forecast-notifier-{Guid.NewGuid():N}");
            var isSubscribed = false;

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!WeatherAppSettingsStore.Get().Enabled)
                {
                    isSubscribed = false;
                    if (mqttClient.IsConnected)
                    {
                        await mqttClient.DisconnectAsync(new MqttClientDisconnectOptions(), stoppingToken);
                        _logger.LogInformation("Disconnected weather forecast notifier because MQTT is disabled.");
                    }

                    await Task.Delay(100, stoppingToken);
                    continue;
                }

                try
                {
                    if (!mqttClient.IsConnected)
                    {
                        isSubscribed = false;
                        await mqttClient.ConnectAsync(clientOptions, stoppingToken);
                    }

                    if (!isSubscribed)
                    {
                        await mqttClient.SubscribeAsync(
                            new MqttTopicFilterBuilder()
                                .WithTopic(_mqttOptions.Topic)
                                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                                .Build(),
                            stoppingToken);
                        isSubscribed = true;
                        _logger.LogInformation("Subscribed weather forecast notifier to MQTT topic {Topic}.",
                            _mqttOptions.Topic);
                    }

                    await Task.Delay(100, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    if (WeatherAppSettingsStore.Get().Enabled)
                    {
                        _logger.LogError(exception, "MQTT notifier connection failed for {Host}:{Port}; retrying in {RetrySeconds} seconds.",
                            _mqttOptions.Host, _mqttOptions.Port, _mqttOptions.ReconnectDelaySeconds);
                        var retryAt = DateTimeOffset.UtcNow.AddSeconds(_mqttOptions.ReconnectDelaySeconds);
                        while (DateTimeOffset.UtcNow < retryAt &&
                               WeatherAppSettingsStore.Get().Enabled)
                        {
                            await Task.Delay(100, stoppingToken);
                        }
                    }
                }
            }
        }

        private async Task HandleMessageAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            WeatherForecast? forecast;
            try
            {
                forecast = JsonSerializer.Deserialize<WeatherForecast>(
                    eventArgs.ApplicationMessage.Payload.ToArray(),
                    JsonOptions);
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "Ignoring invalid weather forecast received from MQTT topic {Topic}.",
                    eventArgs.ApplicationMessage.Topic);
                return;
            }

            if (forecast is null)
            {
                _logger.LogWarning("Ignoring empty weather forecast received from MQTT topic {Topic}.",
                    eventArgs.ApplicationMessage.Topic);
                return;
            }

            try
            {
                _logger.LogInformation("Broadcasting weather forecast received from MQTT topic {Topic}.",
                    eventArgs.ApplicationMessage.Topic);
                await _hubContext.Clients.All.ReceiveWeatherForecast(forecast);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to broadcast weather forecast received from MQTT topic {Topic}.",
                    eventArgs.ApplicationMessage.Topic);
            }
        }
    }
}
