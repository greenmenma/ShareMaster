import { Component, type ErrorInfo, type ReactNode } from 'react';
import { reportClientError } from '../utils/clientLog';

interface State {
  error: Error | null;
}

/** 画面の描画中に発生した例外を捕捉し、真っ白にせずメッセージを表示してサーバーへ記録する */
export class ErrorBoundary extends Component<{ children: ReactNode }, State> {
  state: State = { error: null };

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    reportClientError(`画面の描画でエラーが発生しました：${error.message}`, `${error.stack ?? ''}\n${info.componentStack ?? ''}`);
  }

  render() {
    if (!this.state.error) return this.props.children;
    return (
      <div style={{ padding: 24, fontFamily: 'sans-serif' }}>
        <h1 style={{ fontSize: 20 }}>画面の表示中にエラーが発生しました</h1>
        <p>{this.state.error.message}</p>
        <p>画面を再読み込みしても解消しない場合は、システム管理者に連絡してください（エラー内容はサーバーのログに記録されています）。</p>
        <button type="button" onClick={() => window.location.reload()}>
          再読み込み
        </button>
      </div>
    );
  }
}
