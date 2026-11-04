import { useState } from 'react';
import { Avatar, Box, Chip, Divider, IconButton, ListItemIcon, Menu, MenuItem, Tooltip, Typography } from '@mui/material';
import LogoutIcon from '@mui/icons-material/Logout';
import CheckIcon from '@mui/icons-material/Check';
import { useMe } from '../api/queries';
import { useAuth } from '../auth/AuthContext';

export function UserMenu() {
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const { data: me } = useMe();
  const auth = useAuth();
  const isDev = auth.config.mode === 'Development';

  return (
    <>
      <Tooltip title={me?.displayName ?? ''}>
        <IconButton onClick={(e) => setAnchor(e.currentTarget)} sx={{ p: 0.5 }} aria-label="ユーザーメニュー">
          <Avatar sx={{ width: 32, height: 32, bgcolor: 'secondary.main', fontSize: 15 }}>{me?.displayName?.[0] ?? '?'}</Avatar>
        </IconButton>
      </Tooltip>
      <Menu anchorEl={anchor} open={!!anchor} onClose={() => setAnchor(null)}>
        <Box sx={{ px: 2, py: 1, minWidth: 240 }}>
          <Typography sx={{ fontWeight: 700 }}>{me?.displayName}</Typography>
          <Typography variant="body2" color="text.secondary">
            {me?.userKey}
          </Typography>
          {me?.isSystemAdmin && <Chip label="システム管理者" size="small" color="primary" sx={{ mt: 1 }} />}
        </Box>
        <Divider />
        {isDev && (
          <Box>
            <Typography variant="caption" color="text.secondary" sx={{ px: 2 }}>
              開発用ユーザー切替
            </Typography>
            {auth.config.devUsers.map((u) => (
              <MenuItem key={u.userKey} onClick={() => auth.switchDevUser(u.userKey)}>
                <ListItemIcon>{u.userKey === me?.userKey && <CheckIcon fontSize="small" />}</ListItemIcon>
                {u.displayName}
              </MenuItem>
            ))}
          </Box>
        )}
        {!isDev && (
          <MenuItem onClick={auth.signOut}>
            <ListItemIcon>
              <LogoutIcon fontSize="small" />
            </ListItemIcon>
            サインアウト
          </MenuItem>
        )}
      </Menu>
    </>
  );
}
