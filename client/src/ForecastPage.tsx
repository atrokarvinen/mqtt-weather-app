import { useWeatherForecast } from "./useWeatherForecast";

export function ForecastPage() {
  const {
    forecasts,
    receivedAt,
    liveUpdatesEnabled,
    setLiveUpdatesEnabled,
    isLoading,
    loadError,
    refreshForecasts,
  } = useWeatherForecast();

  return (
    <section className="forecast-section" aria-live="polite">
      <p className="eyebrow">WEATHER STORAGE</p>
      <h1>Stored forecasts.</h1>
      <p className="intro">
        Forecasts saved by the weather service, with optional live updates.
      </p>

      <div className="forecast-controls">
        <button
          className="refresh-button"
          type="button"
          onClick={() => void refreshForecasts()}
          disabled={isLoading}
        >
          {isLoading ? "Refreshing…" : "Refresh forecasts"}
        </button>
        <label className="live-toggle">
          <input
            type="checkbox"
            checked={liveUpdatesEnabled}
            onChange={(event) => setLiveUpdatesEnabled(event.target.checked)}
          />
          <span>Live updates</span>
        </label>
        <span className="forecast-count">
          {forecasts.length} {forecasts.length === 1 ? "forecast" : "forecasts"}
        </span>
      </div>

      {loadError && (
        <p className="load-error" role="alert">
          Could not load forecasts: {loadError}
        </p>
      )}

      {forecasts.length > 0 ? (
        <div className="forecast-list">
          {[...forecasts].reverse().map((forecast, index) => {
            const formattedDate = new Intl.DateTimeFormat(undefined, {
              dateStyle: "full",
            }).format(new Date(`${forecast.date}T00:00:00`));

            return (
              <article className="forecast-card" key={forecast.id}>
                <div className="forecast-heading">
                  <div>
                    <p className="forecast-date">{formattedDate}</p>
                    <h2>{forecast.summary ?? "Forecast"}</h2>
                  </div>
                  <div className="forecast-reading">
                    <span className="weather-icon" aria-hidden="true">
                      ☁️
                    </span>
                    <div className="temperature">
                      {forecast.temperatureC}
                      <span>°C</span>
                    </div>
                    <p className="fahrenheit">{forecast.temperatureF}°F</p>
                  </div>
                </div>
                <div className="forecast-footer">
                  <span>
                    {index === 0
                      ? "Latest stored forecast"
                      : "Stored forecast"}
                  </span>
                  {index === 0 && receivedAt && (
                    <time dateTime={receivedAt.toISOString()}>
                      Live update at {receivedAt.toLocaleTimeString()}
                    </time>
                  )}
                </div>
              </article>
            );
          })}
        </div>
      ) : (
        <div className="empty-card">
          <span className="loading-icon" aria-hidden="true">
            ⌁
          </span>
          <h2>
            {isLoading ? "Loading stored forecasts" : "No stored forecasts"}
          </h2>
          <p>
            {isLoading
              ? "Fetching forecasts from weather storage…"
              : "New forecasts will appear here when they are stored."}
          </p>
        </div>
      )}
    </section>
  );
}
