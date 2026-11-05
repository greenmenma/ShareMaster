import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Aspire から起動された場合は参照先 Server の URL が環境変数で渡される。
// 単独起動（npm run dev）の場合は Server の既定ポートへプロキシする。
const serverUrl =
  process.env.SERVER_HTTPS ??
  process.env.SERVER_HTTP ??
  process.env.services__server__https__0 ??
  process.env.services__server__http__0 ??
  'http://localhost:5148';

// 配置先のパス（IIS のサブフォルダ等）。例：BASE_PATH=/ShareMaster/
// 未指定ならサイトのルート（/）に配置する前提でビルドする。
function normalizeBase(value: string | undefined): string {
  const trimmed = (value ?? '').trim().replace(/^\/+|\/+$/g, '');
  return trimmed ? `/${trimmed}/` : '/';
}

export default defineConfig({
  base: normalizeBase(process.env.BASE_PATH),
  plugins: [react()],
  server: {
    port: Number(process.env.PORT ?? 5173),
    proxy: {
      '/api': { target: serverUrl, changeOrigin: true, secure: false },
      '/health': { target: serverUrl, changeOrigin: true, secure: false },
    },
  },
  build: {
    outDir: 'dist',
    chunkSizeWarningLimit: 1500,
  },
});
