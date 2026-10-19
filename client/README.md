# MQTT Weather App client

The React client displays the latest forecast broadcast by the ASP.NET Core
SignalR hub.

## Run locally

From the repository root, start the MQTT broker first. The broker requires
local credentials:

```powershell
Copy-Item .\mqtt\.env.example .\mqtt\.env
# Edit .\mqtt\.env and replace the sample password.
docker compose --env-file .\mqtt\.env -f .\mqtt\compose.yaml up -d
```

Set the backend's MQTT credentials to the same values in `mqtt\.env`, then
start it with its HTTP launch profile:

```powershell
$env:Mqtt__Username = "weather-app"
$env:Mqtt__Password = "replace-with-a-local-password"
dotnet run --project .\server\MqttWeatherApp\MqttWeatherApp --launch-profile http
```

The backend connects to `localhost:1883` by default. Override the broker settings
with the `Mqtt__Host`, `Mqtt__Port`, and `Mqtt__Topic` environment variables when
needed. The worker publishes forecasts to MQTT; `WeatherForecastNotifier`
subscribes to the same topic and relays them through the existing SignalR hub.

Start the client in a separate terminal:

```powershell
Set-Location .\client
npm install
npm run dev
```

The Vite development server proxies `/hubs` to `http://localhost:5245`, where
the backend maps the SignalR hub at `/hubs/weatherforecast`.

The Queue diagnostics tab reads the pending in-memory storage queue from
`GET /queue-diagnostics`. Start the storage service on port 5031 for this view:

```powershell
dotnet run --project .\server\MqttWeatherStorage\MqttWeatherStorage --launch-profile http
```

The displayed count is the storage service's pending work queue, not the MQTT
broker's queue. Use Refresh queue to request the latest count.

The Settings tab configures each service independently through
`/app-settings` (port 5245) and `/storage-settings` (port 5031). Settings are
kept in memory and reset when the respective service restarts. Disabling MQTT
disconnects that service's MQTT client; the app's send interval and storage's
simulated processing delay are configured in milliseconds.

To connect the built client directly to a separately hosted backend, set
`VITE_SIGNALR_HUB_URL` to the full hub URL and add the client origin to the
backend's `Cors:AllowedOrigins` setting. For runtime settings served outside
the Vite proxy, set `VITE_APP_SETTINGS_URL` and `VITE_STORAGE_SETTINGS_URL` to
the corresponding service endpoints and allow the client origin in each
service's `Cors:AllowedOrigins` setting.
