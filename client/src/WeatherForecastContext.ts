import { createContext } from "react";

export type WeatherForecast = {
  id: string;
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string | null;
};

export type ConnectionStatus =
  | "disabled"
  | "connecting"
  | "connected"
  | "reconnecting"
  | "disconnected";

export type WeatherForecastContextValue = {
  forecasts: WeatherForecast[];
  receivedAt: Date | null;
  connectionStatus: ConnectionStatus;
  liveUpdatesEnabled: boolean;
  setLiveUpdatesEnabled: (enabled: boolean) => void;
  isLoading: boolean;
  loadError: string | null;
  refreshForecasts: () => Promise<void>;
};

export const WeatherForecastContext =
  createContext<WeatherForecastContextValue | null>(null);
