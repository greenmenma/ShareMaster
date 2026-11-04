# 05. API 一覧

すべて `/api` 配下。`auth-config` 以外は認証必須です。エラーは ProblemDetails（`title` にメッセージ）で返します。
開発環境では `/scalar` で API リファレンスを参照できます。

| メソッド | パス | 内容 | 権限 |
| --- | --- | --- | --- |
| GET | `/api/auth-config` | SPA 用の認証設定 | 匿名 |
| GET | `/api/me` | ログインユーザー情報（利用者マスタへ自動登録） | |
| GET | `/api/users?search=` | 利用者検索 | |
| GET | `/api/calendars` | カレンダー一覧（継承後の定休曜日・編集可否を含む） | |
| GET | `/api/calendars/{id}` | カレンダー詳細 | |
| POST | `/api/calendars` | カレンダー追加 | システム管理者 |
| PUT | `/api/calendars/{id}` | カレンダー設定変更 | 管理者 |
| DELETE | `/api/calendars/{id}` | カレンダー削除（論理削除・子がある場合は不可） | システム管理者 |
| GET | `/api/calendars/days?from=&to=&ids=` | 複数カレンダーの確定済み日情報 | |
| GET | `/api/calendars/{id}/days?from=&to=` | 単一カレンダーの確定済み日情報 | |
| GET | `/api/calendars/{id}/days/{date}` | 日詳細（現在・親・自身の設定） | |
| PUT | `/api/calendars/{id}/days` | 日別設定（単日・期間・曜日指定、継承に戻す） | 管理者 |
| GET | `/api/calendars/year-range` | データが存在する年の範囲 | |
| GET/POST/PUT/DELETE | `/api/day-types` | 日種別マスタ | 更新はシステム管理者 |
| GET | `/api/public-holidays?year=` | 祝日一覧 | |
| POST | `/api/public-holidays/generate?year=` | 祝日の自動生成 | システム管理者 |
| POST/PUT/DELETE | `/api/public-holidays` | 祝日の追加・変更・削除 | システム管理者 |
| GET | `/api/histories?calendarId=&entityType=&keyword=&page=&pageSize=` | 変更履歴 | |
| GET | `/api/notifications` | 自分宛ての通知 | |
| POST | `/api/notifications/{id}/read`, `/api/notifications/read-all` | 既読化 | |
| GET/POST/DELETE | `/api/system-admins` | システム管理者 | システム管理者 |
| GET | `/health` | ヘルスチェック | 匿名 |

## 外部システムからの利用

他システムは `GET /api/calendars/days?from=2026-01-01&to=2026-12-31&ids=3` で、親子関係を解決済みの休日情報（`isHoliday`）を取得できます。
サービス間連携ではクライアント資格情報フロー（アプリケーション権限）の追加を検討してください。
