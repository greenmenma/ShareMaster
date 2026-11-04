# IL共有マスター（ShareMaster）

各システムで共有するマスタ情報を一元管理する Web システムです。
第一弾として **カレンダーマスタ** を実装しています（口座・発注担当コード・取引先・受注型式は今後追加予定のダミーメニュー）。

## 技術構成

| 区分 | 採用技術 |
| --- | --- |
| バックエンド | .NET 10 / ASP.NET Core Web API（`src/ShareMaster.Server`） |
| フロントエンド | React 19 + TypeScript + Vite + MUI（`src/ShareMaster.Client`） |
| ローカル開発 | .NET Aspire 13（`src/ShareMaster.AppHost`）※本番構成には含めない・コンテナ不使用 |
| DB | SQLite（現在）→ SQL Server / Azure SQL Database へ移行可能 |
| 認証 | Microsoft Entra ID（本番） / 擬似ログイン（開発） |

## クイックスタート

前提：.NET 10 SDK、Node.js 22 以上（Docker は不要）

```bash
dotnet run --project src/ShareMaster.AppHost
```

Aspire ダッシュボードが開き、Server → Client の順に起動します。http://localhost:5173 を開くと画面が表示されます。

VS Code では「実行とデバッグ」から **全体: Server + Vite + Chrome** を選んで F5 を押します。Visual Studio / Rider では `ShareMaster.slnx` を開き、`ShareMaster.AppHost` をスタートアップにしてください（Windows でも同様）。

詳細は [docs](docs/) を参照してください。

| ドキュメント | 内容 |
| --- | --- |
| [01_システム概要](docs/01_システム概要.md) | 構成・フォルダ構成・画面一覧 |
| [02_カレンダーマスタ仕様](docs/02_カレンダーマスタ仕様.md) | 親子関係・配色・権限・通知・履歴 |
| [03_開発環境](docs/03_開発環境.md) | Mac / Windows でのセットアップとデバッグ |
| [04_本番環境構築](docs/04_本番環境構築.md) | Azure・Entra ID・DB の設定とデプロイ |
| [05_API一覧](docs/05_API一覧.md) | Web API の一覧 |

## テスト

```bash
dotnet test ShareMaster.slnx               # サーバー（xUnit）
npm --prefix src/ShareMaster.Client test   # クライアント（Vitest）
```

## ライセンス

[MIT](LICENSE)
