import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Vite escucha en 0.0.0.0 para que sea accesible desde fuera del contenedor.
export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',
    port: 3000,
    strictPort: true,
    watch: {
      usePolling: true
    }
  },
  preview: {
    host: '0.0.0.0',
    port: 3000
  }
})
