import { useState } from 'react';
import { useNavigate } from 'react-router';
import {
  Badge,
  Box,
  Button,
  Divider,
  IconButton,
  List,
  ListItemButton,
  ListItemText,
  Popover,
  Tooltip,
  Typography,
} from '@mui/material';
import NotificationsIcon from '@mui/icons-material/Notifications';
import { useNotificationMutations, useNotifications } from '../api/queries';
import { fmtDateTime } from '../utils/date';

/** 親カレンダー更新などのアプリ内通知 */
export function NotificationBell() {
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const { data } = useNotifications();
  const { markRead, markAllRead } = useNotificationMutations();
  const navigate = useNavigate();

  return (
    <>
      <Tooltip title="通知">
        <IconButton color="inherit" onClick={(e) => setAnchor(e.currentTarget)} aria-label="通知">
          <Badge badgeContent={data?.unreadCount ?? 0} color="error" max={99}>
            <NotificationsIcon />
          </Badge>
        </IconButton>
      </Tooltip>
      <Popover
        open={!!anchor}
        anchorEl={anchor}
        onClose={() => setAnchor(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{ paper: { sx: { width: { xs: 'calc(100vw - 32px)', sm: 400 }, maxHeight: 480 } } }}
      >
        <Box sx={{ display: 'flex', alignItems: 'center', px: 2, py: 1 }}>
          <Typography variant="subtitle1" sx={{ flex: 1, fontWeight: 700 }}>
            通知
          </Typography>
          <Button size="small" disabled={!data?.unreadCount} onClick={() => markAllRead.mutate()}>
            すべて既読
          </Button>
        </Box>
        <Divider />
        {!data?.items.length ? (
          <Typography sx={{ p: 3, color: 'text.secondary' }} variant="body2">
            通知はありません
          </Typography>
        ) : (
          <List dense disablePadding>
            {data.items.map((n) => (
              <ListItemButton
                key={n.id}
                sx={{ alignItems: 'flex-start', bgcolor: n.isRead ? undefined : 'rgba(81,72,157,0.06)' }}
                onClick={() => {
                  if (!n.isRead) markRead.mutate(n.id);
                  setAnchor(null);
                  if (n.linkUrl) navigate(n.linkUrl);
                }}
              >
                <ListItemText
                  primary={n.title}
                  secondary={
                    <>
                      <Typography component="span" variant="body2" sx={{ display: 'block' }}>
                        {n.message}
                      </Typography>
                      <Typography component="span" variant="caption" color="text.secondary">
                        {fmtDateTime(n.createdAt)}
                      </Typography>
                    </>
                  }
                  slotProps={{ primary: { sx: { fontWeight: n.isRead ? 400 : 700, fontSize: 14 } } }}
                />
              </ListItemButton>
            ))}
          </List>
        )}
      </Popover>
    </>
  );
}
