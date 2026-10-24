import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: process.env.FOGO_API_URL ?? 'https://localhost:7001',
        changeOrigin: true,
        secure: process.env.FOGO_API_INSECURE_TLS !== 'true',
      },
      '/hubs': {
        target: process.env.FOGO_API_URL ?? 'https://localhost:7001',
        changeOrigin: true,
        secure: process.env.FOGO_API_INSECURE_TLS !== 'true',
        ws: true,
      },
    },
  },
});