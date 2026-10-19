namespace MqttWeatherApp
{
    public static class WeatherForecastFactory
    {
        private const int MinimumTemperatureC = -20;
        private const int MaximumTemperatureC = 54;
        private static readonly object StateLock = new();
        private static readonly string[] Summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];
        private static int _temperatureC = Random.Shared.Next(MinimumTemperatureC, MaximumTemperatureC + 1);
        private static string _summary = Summaries[Random.Shared.Next(Summaries.Length)];
        private static int _summaryChangeChancePercent = 1;

        public static WeatherForecast Create(DateOnly date)
        {
            lock (StateLock)
            {
                var step = Random.Shared.Next(2) == 0 ? -1 : 1;
                var nextTemperature = _temperatureC + step;
                if (nextTemperature < MinimumTemperatureC || nextTemperature > MaximumTemperatureC)
                {
                    step = -step;
                }
                _temperatureC += step;

                if (Random.Shared.Next(100) < _summaryChangeChancePercent)
                {
                    var nextSummaryIndex = Random.Shared.Next(Summaries.Length - 1);
                    var currentSummaryIndex = Array.IndexOf(Summaries, _summary);
                    if (nextSummaryIndex >= currentSummaryIndex)
                    {
                        nextSummaryIndex++;
                    }

                    _summary = Summaries[nextSummaryIndex];
                    _summaryChangeChancePercent = 1;
                }
                else
                {
                    _summaryChangeChancePercent = Math.Min(100, _summaryChangeChancePercent + 1);
                }

                return new WeatherForecast
                {
                    Date = date,
                    TemperatureC = _temperatureC,
                    Summary = _summary
                };
            }
        }
    }
}
