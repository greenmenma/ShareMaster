import { createContext, useContext } from 'react';
import type { AuthContextValue } from './bootstrap';

export const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuth(): AuthContextValue {
  const v = useContext(AuthContext);
  if (!v) throw new Error('AuthContext が設定されていません。');
  return v;
}
