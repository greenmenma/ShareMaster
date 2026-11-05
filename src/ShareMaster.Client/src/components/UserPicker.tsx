import { useEffect, useState } from 'react';
import { Autocomplete, Box, CircularProgress, TextField, Typography } from '@mui/material';
import { searchUsers } from '../api/queries';
import type { User } from '../api/types';

interface Props {
  label: string;
  onSelect: (user: User) => void;
  excludeKeys?: string[];
}

/**
 * 利用者の検索選択。
 * サーバーが認証方式に応じて Active Directory（Windows 認証）または Microsoft Graph（Entra ID）から検索し、
 * 一覧から選択する。
 */
export function UserPicker({ label, onSelect, excludeKeys = [] }: Props) {
  const [input, setInput] = useState('');
  const [options, setOptions] = useState<User[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const term = input.trim();
    if (!term) {
      setOptions([]);
      setError(null);
      return;
    }
    let cancelled = false;
    setLoading(true);
    const t = setTimeout(() => {
      searchUsers(term)
        .then((users) => {
          if (cancelled) return;
          setOptions(users.filter((u) => !excludeKeys.includes(u.userKey)));
          setError(null);
        })
        .catch((e: unknown) => {
          if (cancelled) return;
          setOptions([]);
          setError(e instanceof Error ? e.message : String(e));
        })
        .finally(() => !cancelled && setLoading(false));
    }, 300);
    return () => {
      cancelled = true;
      clearTimeout(t);
    };
    // excludeKeys は配列のため文字列化して比較
  }, [input, excludeKeys.join(',')]); // eslint-disable-line

  return (
    <Autocomplete<User>
      options={options}
      value={null}
      inputValue={input}
      onInputChange={(_, v, reason) => reason !== 'reset' && setInput(v)}
      getOptionLabel={(o) => o.displayName}
      isOptionEqualToValue={(a, b) => a.userKey === b.userKey}
      filterOptions={(x) => x}
      loading={loading}
      loadingText="検索中…"
      noOptionsText={error ?? (input.trim() ? '該当する利用者がいません' : '氏名・ユーザー名・メールアドレスを入力してください')}
      onChange={(_, v) => {
        if (!v) return;
        onSelect(v);
        setInput('');
        setOptions([]);
      }}
      renderOption={({ key, ...props }, o) => (
        <Box component="li" key={o.userKey} {...props}>
          <Box sx={{ minWidth: 0 }}>
            <Typography noWrap>
              {o.displayName}
              {o.department && (
                <Typography component="span" variant="body2" color="text.secondary" sx={{ ml: 1 }}>
                  {o.department}
                </Typography>
              )}
            </Typography>
            <Typography variant="caption" color="text.secondary" noWrap component="div">
              {o.userKey}
              {o.email && o.email !== o.userKey ? `／${o.email}` : ''}
            </Typography>
          </Box>
        </Box>
      )}
      renderInput={(params) => (
        <TextField
          {...params}
          label={label}
          size="small"
          placeholder="氏名・ユーザー名・メールアドレスで検索"
          error={!!error}
          slotProps={{
            ...params.slotProps,
            input: {
              ...params.slotProps.input,
              endAdornment: (
                <>
                  {loading && <CircularProgress color="inherit" size={18} />}
                  {params.slotProps.input.endAdornment}
                </>
              ),
            },
          }}
        />
      )}
      sx={{ minWidth: 260, flex: 1 }}
    />
  );
}
