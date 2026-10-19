using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using System.Buffers;
using System.Text.Json;

namespace MqttWeatherStorage
{
    public class MqttWeatherClient(
        ILogger<MqttWeatherClient> logger,
        IOptions<MqttOptions> mqttIOptions
    ) : BackgroundService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly MqttOptions mqttOptions = mqttIOptions.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new MqttClientFactory();
            using var mqttClient = factory.CreateMqttClient();
            mqttClient.ApplicationMessageReceivedAsync += HandleMessageAsync;

            var clientOptions = new MqttClientOptionsBuilder()
                .WithClientId($"weather-storage-{Guid.NewGuid():N}")
                .WithTcpServer(mqttOptions.Host, mqttOptions.Port)
                .WithCredentials(mqttOptions.Username, mqttOptions.Password)
                .WithCleanSession()
                .Build();
            var isSubscribed = false;

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!WeatherStorageSettingsStore.Get().Enabled)
                {
                    isSubscribed = false;
                    if (mqttClient.IsConnected)
                    {
                        await mqttClient.DisconnectAsync(new MqttClientDisconnectOptions(), stoppingToken);
                        logger.LogInformation("Disconnected weather storage client because MQTT is disabled.");
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
                        logger.LogInformation("Connected weather storage client to MQTT broker {Host}:{Port}.",
                            mqttOptions.Host, mqttOptions.Port);
                    }

                    if (!isSubscribed)
                    {
                        await mqttClient.SubscribeAsync(
                            new MqttTopicFilterBuilder()
                                .WithTopic(mqttOptions.Topic)
                                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                                .Build(),
                            stoppingToken);
                        isSubscribed = true;
                        logger.LogInformation("Subscribed weather storage client to MQTT topic {Topic}.",
                            mqttOptions.Topic);
                    }

                    await Task.Delay(100, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    if (WeatherStorageSettingsStore.Get().Enabled)
                    {
                        logger.LogError(exception,
                            "MQTT storage client connection failed for {Host}:{Port}; retrying in {RetrySeconds} seconds.",
                            mqttOptions.Host, mqttOptions.Port, mqttOptions.ReconnectDelaySeconds);
                        var retryAt = DateTimeOffset.UtcNow.AddSeconds(mqttOptions.ReconnectDelaySeconds);
                        while (DateTimeOffset.UtcNow < retryAt &&
                               WeatherStorageSettingsStore.Get().Enabled)
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
                logger.LogWarning(exception, "Ignoring invalid weather forecast received from MQTT topic {Topic}.",
                    eventArgs.ApplicationMessage.Topic);
                return;
            }

            if (forecast is null)
            {
                logger.LogWarning("Ignoring empty weather forecast received from MQTT topic {Topic}.",
                    eventArgs.ApplicationMessage.Topic);
                return;
            }

            MqttWeatherStoreQueue.Enqueue(forecast);
        }
    }
}