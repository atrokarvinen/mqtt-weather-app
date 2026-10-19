import { useEffect, useState, type FormEvent } from "react";

type RuntimeSettings = {
  enabled: boolean;
  milliseconds: number;
};

type SettingsCardProps = {
  title: string;
  description: string;
  endpoint: string;
  intervalLabel: string;
  intervalField: "sendInterval" | "storeTime";
};

function SettingsCard({
  title,
  description,
  endpoint,
  intervalLabel,
  intervalField,
}: SettingsCardProps) {
  const [settings, setSettings] = useState<RuntimeSettings | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    const controller = new AbortController();

    const loadSettings = async () => {
      try {
        const response = await fetch(endpoint, { signal: controller.signal });
        if (!response.ok) {
          throw new Error(`Settings endpoint returned ${response.status}.`);
        }

        const result = (await response.json()) as {
          enabled?: unknown;
          [key: string]: unknown;
        };
        const milliseconds = result[intervalField];
        if (
          typeof result.enabled !== "boolean" ||
          typeof milliseconds !== "number" ||
          !Number.isInteger(milliseconds) ||
          milliseconds < 1
        ) {
          throw new Error("Settings endpoint returned invalid values.");
        }

        if (!controller.signal.aborted) {
          setSettings({ enabled: result.enabled, milliseconds });
          setError(null);
        }
      } catch (loadError) {
        if (!controller.signal.aborted) {
          setError(
            loadError instanceof Error
              ? loadError.message
              : "Unable to load settings.",
          );
        }
      } finally {
        if (!controller.signal.aborted) setIsLoading(false);
      }
    };

    void loadSettings();
    return () => controller.abort();
  }, [endpoint, intervalField]);

  const saveSettings = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!settings) return;

    setIsSaving(true);
    setError(null);
    setSaved(false);
    try {
      const response = await fetch(endpoint, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          enabled: settings.enabled,
          [intervalField]: settings.milliseconds,
        }),
      });
      if (!response.ok) {
        throw new Error(`Saving settings returned ${response.status}.`);
      }
      setSaved(true);
    } catch (saveError) {
      setError(
        saveError instanceof Error ? saveError.message : "Unable to save settings.",
      );
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <article className="settings-card">
      <div>
        <h2>{title}</h2>
        <p>{description}</p>
      </div>
      {isLoading ? (
        <p className="settings-status">Loading settings…</p>
      ) : settings ? (
        <form onSubmit={(event) => void saveSettings(event)}>
          <label className="settings-enabled">
            <span>
              <strong>Enabled</strong>
              <small>
                {settings.enabled
                  ? "MQTT client can connect and subscribe."
                  : "MQTT client disconnects and stops subscribing."}
              </small>
            </span>
            <input
              type="checkbox"
              checked={settings.enabled}
              onChange={(event) => {
                setSettings({ ...settings, enabled: event.target.checked });
                setSaved(false);
              }}
            />
          </label>
          <label className="settings-interval">
            <span>{intervalLabel} (ms)</span>
            <input
              type="number"
              min="1"
              step="1"
              required
              value={settings.milliseconds}
              onChange={(event) => {
                const milliseconds = Number(event.target.value);
                setSettings({ ...settings, milliseconds });
                setSaved(false);
              }}
            />
          </label>
          {error && (
            <p className="load-error" role="alert">
              {error}
            </p>
          )}
          {saved && (
            <p className="settings-success" role="status">
              Settings saved.
            </p>
          )}
          <button
            className="refresh-button"
            type="submit"
            disabled={
              isSaving ||
              !Number.isInteger(settings.milliseconds) ||
              settings.milliseconds < 1
            }
          >
            {isSaving ? "Saving…" : "Save settings"}
          </button>
        </form>
      ) : (
        <p className="load-error" role="alert">
          {error ?? "Settings are unavailable."}
        </p>
      )}
    </article>
  );
}

export function SettingsPage() {
  return (
    <section className="settings-section">
      <p className="eyebrow">RUNTIME CONFIGURATION</p>
      <h1>Settings.</h1>
      <p className="intro">
        These settings are kept in memory and reset when the service restarts.
      </p>
      <div className="settings-list">
        <SettingsCard
          title="MqttWeatherApp"
          description="Controls forecast publishing and the app's MQTT notifier."
          endpoint={import.meta.env.VITE_APP_SETTINGS_URL || "/app-settings"}
          intervalLabel="Send interval"
          intervalField="sendInterval"
        />
        <SettingsCard
          title="MqttWeatherStorage"
          description="Controls the MQTT storage subscription and simulated processing delay."
          endpoint={
            import.meta.env.VITE_STORAGE_SETTINGS_URL || "/storage-settings"
          }
          intervalLabel="Store time"
          intervalField="storeTime"
        />
      </div>
    </section>
  );
}
