namespace MqttWeatherApp
{
    public sealed record WeatherAppSettings(bool Enabled, int SendInterval);

    public static class WeatherAppSettingsStore
    {
        private static WeatherAppSettings _settings = new(Enabled: true, SendInterval: 1000);

        public static WeatherAppSettings Get()
        {
            return Volatile.Read(ref _settings);
        }

        public static void Update(WeatherAppSettings settings)
        {
            Interlocked.Exchange(ref _settings, settings);
        }
    }
}
