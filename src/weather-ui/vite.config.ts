import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),   // D-02: Tailwind v4 Vite-native plugin, replaces postcss pipeline
  ],
  server: {
    proxy: {
      '/api': 'http://localhost:5055'   // D-14 / PITFALL §4: avoids HTTP→HTTPS redirect; PITFALL §8: eliminates CORS dependency
    }
  }
})
