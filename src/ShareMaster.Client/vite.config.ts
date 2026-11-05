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

// 本番ビルドは相対パス（./assets/...）で出力し、配置先のパス（IIS のサブフォルダ等）は
// サーバーが index.html に埋め込む <base href> で実行時に決まる。開発サーバーはルート（/）。
export default defineConfig(({ command }) => ({
  base: command === 'build' ? './' : '/',
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
}));
