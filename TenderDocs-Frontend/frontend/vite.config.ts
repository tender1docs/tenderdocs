import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

// In local dev (`npm run dev`) the SPA calls /api, /swagger, /health on the same origin;
// these are proxied to the backend gateway so you don't need CORS or absolute URLs.
const API_TARGET = process.env.VITE_PROXY_TARGET || 'http://localhost:8080'

export default defineConfig({
  plugins: [react()],
  resolve: { alias: { '@': path.resolve(__dirname, './src') } },
  server: {
    port: 5173,
    proxy: {
      '/api': { target: API_TARGET, changeOrigin: true },
      '/swagger': { target: API_TARGET, changeOrigin: true },
      '/health': { target: API_TARGET, changeOrigin: true },
    },
  },
})
