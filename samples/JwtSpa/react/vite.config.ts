import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

export default defineConfig({
  root: 'react',
  plugins: [react()],
  build: { outDir: '../dist/react', emptyOutDir: true },
  server: {
    host: '127.0.0.1',
    port: 5173,
    strictPort: true,
    proxy: { '/api/': 'http://127.0.0.1:5080' },
  },
});
