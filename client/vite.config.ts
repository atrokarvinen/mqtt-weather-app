import babel from "@rolldown/plugin-babel";
import react, { reactCompilerPreset } from "@vitejs/plugin-react";
import { defineConfig } from "vite";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), babel({ presets: [reactCompilerPreset()] })],
  server: {
    proxy: {
      "/app-settings": {
        target: "http://localhost:5245",
      },
      "/queue-diagnostics": {
        target: "http://localhost:5031",
      },
      "/storage-settings": {
        target: "http://localhost:5031",
      },
      "/weather-storage": {
        target: "http://localhost:5031",
      },
      "/hubs": {
        target: "http://localhost:5245",
        ws: true,
      },
    },
  },
});
