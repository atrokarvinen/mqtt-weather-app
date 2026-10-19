import { useEffect, useState } from "react";

type QueueDiagnostics = {
  queueSize: number;
};

export function QueueDiagnosticsPage() {
  const [queueSize, setQueueSize] = useState<number | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [refreshKey, setRefreshKey] = useState(0);

  useEffect(() => {
    const controller = new AbortController();

    const loadQueueSize = async () => {
      setIsLoading(true);
      setError(null);

      try {
        const response = await fetch(
          import.meta.env.VITE_QUEUE_DIAGNOSTICS_URL || "/queue-diagnostics",
          { signal: controller.signal },
        );
        if (!response.ok) {
          throw new Error(`Queue diagnostics returned ${response.status}.`);
        }

        const diagnostics = (await response.json()) as QueueDiagnostics;
        if (!Number.isInteger(diagnostics.queueSize) || diagnostics.queueSize < 0) {
          throw new Error("Queue diagnostics returned an invalid queue size.");
        }

        setQueueSize(diagnostics.queueSize);
        setLastUpdated(new Date());
      } catch (loadError) {
        if (!controller.signal.aborted) {
          setError(
            loadError instanceof Error
              ? loadError.message
              : "Unable to load queue diagnostics.",
          );
        }
      } finally {
        if (!controller.signal.aborted) setIsLoading(false);
      }
    };

    void loadQueueSize();
    return () => controller.abort();
  }, [refreshKey]);

  return (
    <section className="diagnostics-section" aria-live="polite">
      <p className="eyebrow">WEATHER STORAGE</p>
      <div className="diagnostics-heading">
        <div>
          <h1>Queue diagnostics.</h1>
          <p className="intro">
            Pending forecasts waiting to be stored by this service.
          </p>
        </div>
        <button
          className="refresh-button"
          type="button"
          onClick={() => setRefreshKey((current) => current + 1)}
          disabled={isLoading}
        >
          {isLoading ? "Refreshing…" : "Refresh queue"}
        </button>
      </div>

      {error && (
        <p className="load-error" role="alert">
          Could not load queue size: {error}
        </p>
      )}

      <article className="queue-size-card">
        <p>Items currently in the storage queue</p>
        <strong>{queueSize ?? "—"}</strong>
        <span>
          {lastUpdated
            ? `Last updated ${lastUpdated.toLocaleTimeString()}`
            : isLoading
              ? "Loading queue size…"
              : "Queue size unavailable"}
        </span>
      </article>
      <p className="diagnostics-note">
        This is the storage service’s in-memory pending queue, not messages
        waiting in the MQTT broker.
      </p>
    </section>
  );
}
