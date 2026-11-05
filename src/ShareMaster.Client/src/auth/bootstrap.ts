// アプリ起動時の認証初期化。
// 本番（EntraId）は MSAL でサインインし、開発（Development）は擬似ユーザーで動作する。
import { InteractionRequiredAuthError, PublicClientApplication, type AccountInfo } from '@azure/msal-browser';
import { getStoredDevUser, setDevUser, setTokenProvider, withBase } from '../api/http';
import type { AuthConfig } from '../api/types';

export interface AuthContextValue {
  config: AuthConfig;
  account: AccountInfo | null;
  signOut: () => void;
  switchDevUser: (userKey: string) => void;
}

export async function bootstrapAuth(): Promise<AuthContextValue> {
  const res = await fetch(withBase('/api/auth-config'));
  if (!res.ok) throw new Error(`認証設定の取得に失敗しました（${res.status}）`);
  const config = (await res.json()) as AuthConfig;

  if (config.mode === 'Development') {
    const stored = getStoredDevUser();
    const user = config.devUsers.find((x) => x.userKey === stored) ?? config.devUsers[0];
    setDevUser(user?.userKey ?? null);
    return {
      config,
      account: null,
      signOut: () => undefined,
      switchDevUser: (userKey) => {
        setDevUser(userKey);
        window.location.reload();
      },
    };
  }

  if (!config.clientId || !config.authority) throw new Error('Entra ID の設定（Auth:Spa）が不足しています。');
  // サブフォルダ配置時もアプリのトップへ戻す（Entra ID のリダイレクト URI と一致させること）
  const redirectUri = `${window.location.origin}${withBase('/')}`;
  const pca = new PublicClientApplication({
    auth: { clientId: config.clientId, authority: config.authority, redirectUri, postLogoutRedirectUri: redirectUri },
    cache: { cacheLocation: 'localStorage' },
  });
  await pca.initialize();
  const result = await pca.handleRedirectPromise();
  if (result?.account) pca.setActiveAccount(result.account);
  let account = pca.getActiveAccount() ?? pca.getAllAccounts()[0] ?? null;
  if (!account) {
    await pca.loginRedirect({ scopes: config.scopes });
    // リダイレクトするため以降は実行されない
    return new Promise(() => undefined);
  }
  pca.setActiveAccount(account);

  setTokenProvider(async () => {
    account = pca.getActiveAccount() ?? account;
    try {
      const token = await pca.acquireTokenSilent({ scopes: config.scopes, account: account! });
      return token.accessToken;
    } catch (e) {
      if (e instanceof InteractionRequiredAuthError) {
        await pca.acquireTokenRedirect({ scopes: config.scopes, account: account! });
      }
      throw e;
    }
  });

  return {
    config,
    account,
    signOut: () => void pca.logoutRedirect({ account }),
    switchDevUser: () => undefined,
  };
}
