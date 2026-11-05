// 画面で発生したエラーをサーバーのログ（logs/sharemaster-yyyyMMdd.log）へ送る。
// 送信に失敗しても画面の動作には影響させない。
import { withBase } from '../api/http';
import type { ClientLog } from '../api/types';

export function reportClientError(message: string, detail?: unknown, level: ClientLog['level'] = 'error') {
  const body: ClientLog = {
    level,
    message,
    detail: detail instanceof Error ? (detail.stack ?? detail.message) : detail == null ? null : String(detail),
    url: window.location.href,
  };
  try {
    void fetch(withBase('/api/client-logs'), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
      keepalive: true,
    }).catch(() => undefined);
  } catch {
    // 送信できない環境では何もしない
  }
}
