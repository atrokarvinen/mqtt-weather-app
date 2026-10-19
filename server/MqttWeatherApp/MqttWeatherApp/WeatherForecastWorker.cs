using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using System.Text.Json;

namespace MqttWeatherApp
{
    public class WeatherForecastWorker : BackgroundService
    {
        private readonly MqttOptions _mqttOptions;
        private readonly ILogger<WeatherForecastWorker> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public WeatherForecastWorker(
            IOptions<MqttOptions> mqttOptions,
            ILogger<WeatherForecastWorker> logger)
        {
            _mqttOptions = mqttOptions.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new MqttClientFactory();
            using var mqttClient = factory.CreateMqttClient();
            var clientOptions = MqttOptionsFactory.CreateClientOptions(
                _mqttOptions,
                $"weather-forecast-worker-{Guid.NewGuid():N}");
            var lastPublishedAt = DateTimeOffset.MinValue;

            while (!stoppingToken.IsCancellationRequested)
            {
                var settings = WeatherAppSettingsStore.Get();
                if (!settings.Enabled)
                {
                    if (mqttClient.IsConnected)
                    {
                        await mqttClient.DisconnectAsync(new MqttClientDisconnectOptions(), stoppingToken);
                        _logger.LogInformation("Disconnected weather forecast publisher because MQTT is disabled.");
                    }

                    await Task.Delay(100, stoppingToken);
                    continue;
                }

                if (!mqttClient.IsConnected &&
                    !await ConnectWithRetryAsync(mqttClient, clientOptions, stoppingToken))
                {
                    continue;
                }

                if (lastPublishedAt == DateTimeOffset.MinValue ||
                    DateTimeOffset.UtcNow >= lastPublishedAt.AddMilliseconds(settings.SendInterval))
                {
                    var forecast = WeatherForecastFactory.Create(DateOnly.FromDateTime(DateTime.Now));
                    var payload = JsonSerializer.SerializeToUtf8Bytes(forecast, JsonOptions);
                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic(_mqttOptions.Topic)
                        .WithPayload(payload)
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build();

                    try
                    {
                        await mqttClient.PublishAsync(message, stoppingToken);
                        _logger.LogDebug("Published weather forecast to MQTT topic {Topic}.", _mqttOptions.Topic);
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        _logger.LogError(exception, "Failed to publish weather forecast to MQTT topic {Topic}.", _mqttOptions.Topic);
                    }

                    lastPublishedAt = DateTimeOffset.UtcNow;
                }

                await Task.Delay(100, stoppingToken);
            }
        }

        private async Task<bool> ConnectWithRetryAsync(
            IMqttClient mqttClient,
            MqttClientOptions clientOptions,
            CancellationToken cancellationToken)
        {
            while (!mqttClient.IsConnected && WeatherAppSettingsStore.Get().Enabled)
            {
                try
                {
                    await mqttClient.ConnectAsync(clientOptions, cancellationToken);
                    _logger.LogInformation("Connected weather forecast publisher to MQTT broker {Host}:{Port}.",
                        _mqttOptions.Host, _mqttOptions.Port);
                    return true;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    _logger.LogError(exception, "Unable to connect weather forecast publisher to MQTT broker {Host}:{Port}; retrying in {RetrySeconds} seconds.",
                        _mqttOptions.Host, _mqttOptions.Port, _mqttOptions.ReconnectDelaySeconds);
                    var retryAt = DateTimeOffset.UtcNow.AddSeconds(_mqttOptions.ReconnectDelaySeconds);
                    while (DateTimeOffset.UtcNow < retryAt)
                    {
                        if (!WeatherAppSettingsStore.Get().Enabled)
                        {
                            return false;
                        }
                        await Task.Delay(100, cancellationToken);
                    }
                }
            }

            return mqttClient.IsConnected;
        }
    }
}
