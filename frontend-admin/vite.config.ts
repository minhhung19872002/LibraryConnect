import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'node:path';

// Cấu hình build cho SPA quản trị. Trong môi trường phát triển, mọi lời gọi /api
// được proxy sang backend .NET để tránh phải bật CORS khi chạy trên máy cá nhân.
export default defineConfig({
  // Nginx phục vụ giao diện quản trị dưới đường dẫn /admin/, còn khi chạy dev thì ở gốc.
  // Giá trị này quyết định tiền tố của tài nguyên tĩnh và basename của React Router.
  base: process.env.VITE_BASE_PATH ?? '/',
  plugins: [react()],
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') },
  },
  server: {
    port: 5174,
    proxy: {
      '/api': {
        target: process.env.VITE_DEV_API_TARGET ?? 'http://localhost:8080',
        changeOrigin: true,
      },
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: false,
    rollupOptions: {
      output: {
        // Tách vendor để lần deploy sau chỉ phải tải lại phần thay đổi.
        manualChunks: {
          react: ['react', 'react-dom', 'react-router-dom'],
          antd: ['antd', '@ant-design/icons'],
          query: ['@tanstack/react-query', 'axios'],
        },
      },
    },
  },
});
