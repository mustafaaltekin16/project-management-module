import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    pool: 'vmThreads',
    maxWorkers: 1,
    minWorkers: 1
  }
});
