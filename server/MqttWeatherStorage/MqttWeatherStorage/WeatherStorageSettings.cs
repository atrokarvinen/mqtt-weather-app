namespace MqttWeatherStorage;

public sealed record WeatherStorageSettings(bool Enabled, int StoreTime);

public static class WeatherStorageSettingsStore
{
    private static WeatherStorageSettings _settings = new(Enabled: true, StoreTime: 5000);

    public static WeatherStorageSettings Get()
    {
        return Volatile.Read(ref _settings);
    }

    public static void Update(WeatherStorageSettings settings)
    {
        Interlocked.Exchange(ref _settings, settings);
    }
}
