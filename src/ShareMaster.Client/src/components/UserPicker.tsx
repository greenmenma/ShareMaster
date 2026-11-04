import { useEffect, useState } from 'react';
import { Autocomplete, TextField } from '@mui/material';
import { searchUsers } from '../api/queries';
import type { User } from '../api/types';

interface Props {
  label: string;
  onSelect: (user: User) => void;
  excludeKeys?: string[];
}

/**
 * 利用者の検索選択。一度でもログインした利用者が候補に表示される。
 * 未ログインの利用者はメールアドレス（UPN）を直接入力して Enter で追加できる。
 */
export function UserPicker({ label, onSelect, excludeKeys = [] }: Props) {
  const [input, setInput] = useState('');
  const [options, setOptions] = useState<User[]>([]);

  useEffect(() => {
    const t = setTimeout(() => {
      searchUsers(input)
        .then((users) => setOptions(users.filter((u) => !excludeKeys.includes(u.userKey))))
        .catch(() => setOptions([]));
    }, 250);
    return () => clearTimeout(t);
    // excludeKeys は配列のため文字列化して比較
  }, [input, excludeKeys.join(',')]); // eslint-disable-line

  return (
    <Autocomplete<User, false, false, true>
      freeSolo
      options={options}
      value={null}
      inputValue={input}
      onInputChange={(_, v) => setInput(v)}
      getOptionLabel={(o) => (typeof o === 'string' ? o : `${o.displayName}（${o.userKey}）`)}
      filterOptions={(x) => x}
      onChange={(_, v) => {
        if (!v) return;
        const user = typeof v === 'string' ? { userKey: v.trim().toLowerCase(), displayName: v.trim() } : v;
        if (!user.userKey) return;
        onSelect(user);
        setInput('');
      }}
      renderInput={(params) => <TextField {...params} label={label} size="small" placeholder="氏名・メールアドレスで検索" />}
      sx={{ minWidth: 260, flex: 1 }}
    />
  );
}
