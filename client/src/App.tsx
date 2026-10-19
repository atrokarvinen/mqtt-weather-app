import { NavLink, Outlet, Route, Routes, Navigate } from "react-router";
import "./App.css";
import { QueueDiagnosticsPage } from "./QueueDiagnosticsPage";
import { ForecastPage } from "./ForecastPage";
import { SettingsPage } from "./SettingsPage";
import { useWeatherForecast } from "./useWeatherForecast";

function AppLayout() {
  const { connectionStatus } = useWeatherForecast();

  const getConnectionStatus = () => {
    switch (connectionStatus) {
      case "disabled":
        return "Live updates off";
      case "connected":
        return "Live updates on";
      case "reconnecting":
        return "Reconnecting";
      case "connecting":
        return "Connecting";
      default:
        return "Connection lost";
    }
  };

  return (
    <main className="weather-app">
      <header className="app-header">
        <div className="brand">
          <span className="brand-icon" aria-hidden="true">
            ☀
          </span>
          <span>Weather now</span>
        </div>
        <div className="header-actions">
          <nav className="app-navigation" aria-label="Main navigation">
            <NavLink to="/forecasts">Forecasts</NavLink>
            <NavLink to="/queue-diagnostics">Queue diagnostics</NavLink>
            <NavLink to="/settings">Settings</NavLink>
          </nav>
          <output className={`connection-status ${connectionStatus}`}>
            <span className="status-indicator" />
            {getConnectionStatus()}
          </output>
        </div>
      </header>
      <Outlet />
    </main>
  );
}

function App() {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<Navigate to="/forecasts" replace />} />
        <Route path="forecasts" element={<ForecastPage />} />
        <Route path="queue-diagnostics" element={<QueueDiagnosticsPage />} />
        <Route path="settings" element={<SettingsPage />} />
        <Route path="*" element={<Navigate to="/forecasts" replace />} />
      </Route>
    </Routes>
  );
}

export default App;
