import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { CssBaseline, ThemeProvider } from '@mui/material';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import dayjs from 'dayjs';
import 'dayjs/locale/ja';
import { theme } from './theme';
import { App } from './App';
import { AuthContext } from './auth/AuthContext';
import { bootstrapAuth } from './auth/bootstrap';
import { ApiError } from './api/http';
import { ErrorBoundary } from './components/ErrorBoundary';
import { reportClientError } from './utils/clientLog';

dayjs.locale('ja');

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: (count, error) => !(error instanceof ApiError && error.status < 500) && count < 2,
      refetchOnWindowFocus: false,
    },
  },
});

const root = createRoot(document.getElementById('root')!);

bootstrapAuth()
  .then((auth) => {
    root.render(
      <StrictMode>
        <ThemeProvider theme={theme}>
          <CssBaseline />
          <QueryClientProvider client={queryClient}>
            <AuthContext.Provider value={auth}>
              <ErrorBoundary>
                <App />
              </ErrorBoundary>
            </AuthContext.Provider>
          </QueryClientProvider>
        </ThemeProvider>
      </StrictMode>,
    );
  })
  .catch((e: unknown) => {
    reportClientError(`起動に失敗しました：${e instanceof Error ? e.message : String(e)}`, e);
    root.render(
      <div style={{ padding: 24, fontFamily: 'sans-serif' }}>
        <h1 style={{ fontSize: 20 }}>IL共有マスターを起動できませんでした</h1>
        <p>{e instanceof Error ? e.message : String(e)}</p>
      </div>,
    );
  });
