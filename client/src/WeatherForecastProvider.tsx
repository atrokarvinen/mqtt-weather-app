import { HubConnectionBuilder, type HubConnection } from "@microsoft/signalr";
import { useEffect, useState, type ReactNode } from "react";
import {
  WeatherForecastContext,
  type ConnectionStatus,
  type WeatherForecast,
} from "./WeatherForecastContext";

export function WeatherForecastProvider({ children }: { children: ReactNode }) {
  const [forecasts, setForecasts] = useState<WeatherForecast[]>([]);
  const [receivedAt, setReceivedAt] = useState<Date | null>(null);
  const [connectionStatus, setConnectionStatus] =
    useState<ConnectionStatus>("disabled");
  const [liveUpdatesEnabled, setLiveUpdatesEnabled] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    let active = true;

    const loadInitialForecasts = async () => {
      try {
        const response = await fetch(
          import.meta.env.VITE_WEATHER_STORAGE_URL || "/weather-storage",
          { signal: controller.signal },
        );
        if (!response.ok) {
          throw new Error(`Weather storage returned ${response.status}.`);
        }
        const storedForecasts = (await response.json()) as WeatherForecast[];
        if (active) {
          setForecasts(storedForecasts);
          setLoadError(null);
        }
      } catch (error) {
        if (active && !controller.signal.aborted) {
          setLoadError(
            error instanceof Error
              ? error.message
              : "Unable to load forecasts.",
          );
        }
      } finally {
        if (active) setIsLoading(false);
      }
    };

    void loadInitialForecasts();
    return () => {
      active = false;
      controller.abort();
    };
  }, []);

  const refreshForecasts = async () => {
    setIsLoading(true);
    setLoadError(null);
    try {
      const response = await fetch(
        import.meta.env.VITE_WEATHER_STORAGE_URL || "/weather-storage",
      );
      if (!response.ok) {
        throw new Error(`Weather storage returned ${response.status}.`);
      }
      setForecasts((await response.json()) as WeatherForecast[]);
    } catch (error) {
      setLoadError(
        error instanceof Error ? error.message : "Unable to load forecasts.",
      );
    } finally {
      setIsLoading(false);
    }
  };

  const updateLiveUpdatesEnabled = (enabled: boolean) => {
    setLiveUpdatesEnabled(enabled);
    setConnectionStatus(enabled ? "connecting" : "disabled");
  };

  useEffect(() => {
    if (!liveUpdatesEnabled) return;

    let disposed = false;
    let retryHandle: number | undefined;
    const connection: HubConnection = new HubConnectionBuilder()
      .withUrl(import.meta.env.VITE_SIGNALR_HUB_URL || "/hubs/weatherforecast")
      .withAutomaticReconnect()
      .build();

    const receiveForecast = (latestForecast: WeatherForecast) => {
      setForecasts((currentForecasts) =>
        currentForecasts.some((forecast) => forecast.id === latestForecast.id)
          ? currentForecasts
          : [...currentForecasts, latestForecast],
      );
      setReceivedAt(new Date());
    };

    const scheduleReconnect = () => {
      if (!disposed) {
        retryHandle = window.setTimeout(connect, 5000);
      }
    };

    const connect = async () => {
      if (disposed) return;

      try {
        await connection.start();
        if (!disposed) setConnectionStatus("connected");
      } catch {
        if (!disposed) {
          setConnectionStatus("disconnected");
          scheduleReconnect();
        }
      }
    };

    connection.on("ReceiveWeatherForecast", receiveForecast);
    connection.onreconnecting(() => {
      if (!disposed) setConnectionStatus("reconnecting");
    });
    connection.onreconnected(() => {
      if (!disposed) setConnectionStatus("connected");
    });
    connection.onclose(() => {
      if (!disposed) {
        setConnectionStatus("disconnected");
        scheduleReconnect();
      }
    });

    void connect();

    return () => {
      disposed = true;
      if (retryHandle !== undefined) window.clearTimeout(retryHandle);
      connection.off("ReceiveWeatherForecast", receiveForecast);
      void connection.stop();
    };
  }, [liveUpdatesEnabled]);

  return (
    <WeatherForecastContext.Provider
      value={{
        forecasts,
        receivedAt,
        connectionStatus,
        liveUpdatesEnabled,
        setLiveUpdatesEnabled: updateLiveUpdatesEnabled,
        isLoading,
        loadError,
        refreshForecasts,
      }}
    >
      {children}
    </WeatherForecastContext.Provider>
  );
}
