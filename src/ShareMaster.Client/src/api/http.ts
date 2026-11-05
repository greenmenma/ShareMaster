// fetch のラッパー。認証トークン（本番）または開発ユーザーヘッダー（開発）を付与する。

type TokenProvider = () => Promise<string | null>;

let tokenProvider: TokenProvider | null = null;
let devUserKey: string | null = null;

const DEV_USER_STORAGE_KEY = 'sharemaster.devUser';

export function setTokenProvider(provider: TokenProvider | null) {
  tokenProvider = provider;
}

export function getStoredDevUser(): string | null {
  try {
    return localStorage.getItem(DEV_USER_STORAGE_KEY);
  } catch {
    return null;
  }
}

export function setDevUser(userKey: string | null) {
  devUserKey = userKey;
  try {
    if (userKey) localStorage.setItem(DEV_USER_STORAGE_KEY, userKey);
    else localStorage.removeItem(DEV_USER_STORAGE_KEY);
  } catch {
    // localStorage が使えない環境では保持しない
  }
}

/** API エラー（ProblemDetails の内容を保持） */
export class ApiError extends Error {
  readonly status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

async function toError(res: Response): Promise<ApiError> {
  let message = `エラーが発生しました（${res.status}）`;
  try {
    const body = (await res.json()) as ProblemDetails;
    const validation = body.errors ? Object.values(body.errors).flat() : [];
    message = validation.length > 0 ? validation.join('\n') : (body.title ?? body.detail ?? message);
  } catch {
    // JSON 以外の応答はステータスのみ
  }
  if (res.status === 401) message = 'サインインの有効期限が切れました。画面を再読み込みしてください。';
  return new ApiError(res.status, message);
}

/** 配置先のパス（末尾スラッシュなし。ルート配置なら空文字） */
export const BASE_PATH = import.meta.env.BASE_URL.replace(/\/$/, '');

/** アプリ内の絶対パス（/api/... 等）に配置先のパスを付ける */
export function withBase(path: string): string {
  return `${BASE_PATH}${path}`;
}

export async function api<T>(path: string, init?: RequestInit & { json?: unknown }): Promise<T> {
  const headers = new Headers(init?.headers);
  headers.set('Accept', 'application/json');
  let body = init?.body;
  if (init?.json !== undefined) {
    headers.set('Content-Type', 'application/json');
    body = JSON.stringify(init.json);
  }
  if (tokenProvider) {
    const token = await tokenProvider();
    if (token) headers.set('Authorization', `Bearer ${token}`);
  }
  if (devUserKey) headers.set('X-Dev-User', devUserKey);

  const res = await fetch(withBase(path), { ...init, headers, body });
  if (!res.ok) throw await toError(res);
  if (res.status === 204 || res.headers.get('Content-Length') === '0') return undefined as T;
  return (await res.json()) as T;
}

/** クエリ文字列を組み立てる（undefined / null / 空文字は除外、配列は同名キーで繰り返す） */
export function qs(params: Record<string, string | number | boolean | null | undefined | (string | number)[]>): string {
  const sp = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) {
    if (v === undefined || v === null || v === '') continue;
    if (Array.isArray(v)) v.forEach((x) => sp.append(k, String(x)));
    else sp.append(k, String(v));
  }
  const s = sp.toString();
  return s ? `?${s}` : '';
}
