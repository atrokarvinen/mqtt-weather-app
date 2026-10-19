using MQTTnet;

namespace MqttWeatherApp
{
    public static class MqttOptionsFactory
    {
        public static MqttClientOptions CreateClientOptions(MqttOptions options, string clientId)
        {
            if (options.ReconnectDelaySeconds <= 0)
            {
                throw new InvalidOperationException("Mqtt:ReconnectDelaySeconds must be greater than zero.");
            }

            return new MqttClientOptionsBuilder()
                .WithClientId(clientId)
                .WithTcpServer(options.Host, options.Port)
                .WithCredentials(options.Username, options.Password)
                .WithCleanSession()
                .Build();
        }
    }
}
