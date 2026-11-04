import { useCallback, useState } from 'react';
import { Alert, Snackbar } from '@mui/material';

/** 保存結果などを表示する簡易スナックバー */
export function useSnackbar() {
  const [message, setMessage] = useState<{ text: string; severity: 'success' | 'error' | 'info' } | null>(null);
  const show = useCallback((text: string, severity: 'success' | 'error' | 'info' = 'success') => setMessage({ text, severity }), []);
  const element = (
    <Snackbar
      open={!!message}
      autoHideDuration={5000}
      onClose={() => setMessage(null)}
      anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
    >
      {message ? (
        <Alert severity={message.severity} variant="filled" onClose={() => setMessage(null)} sx={{ whiteSpace: 'pre-line' }}>
          {message.text}
        </Alert>
      ) : undefined}
    </Snackbar>
  );
  return { show, element };
}
