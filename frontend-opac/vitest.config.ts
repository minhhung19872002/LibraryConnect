import { defineConfig, mergeConfig } from 'vitest/config';
import path from 'node:path';
import viteConfig from './vite.config';

// Test settings live apart from the build config so `vite build` stays free of test-only types.
export default mergeConfig(
  viteConfig,
  defineConfig({
    test: {
      globals: true,
      environment: 'jsdom',
      setupFiles: [path.resolve(__dirname, 'src/test/setup.ts')],
      include: ['src/**/*.{test,spec}.{ts,tsx}'],
    },
  }),
);
