import { Link as RouterLink } from 'react-router';
import { Box, Card, CardActionArea, CardContent, Chip, Paper, Stack, Typography } from '@mui/material';
import CalendarMonthIcon from '@mui/icons-material/CalendarMonth';
import dayjs from 'dayjs';
import { useCalendars, useDays, useMe, useNotifications } from '../api/queries';
import type { ResolvedDay } from '../api/types';
import { PageHeader } from '../components/PageHeader';
import { DayChip } from '../components/DayChip';
import { dummyMenus } from '../layout/menu';
import { buildTree, flattenTree } from '../utils/calendarTree';
import { fmt, fmtDateTime, fmtJp } from '../utils/date';

/** ホーム：本日の各カレンダーの稼働状況・通知・メニュー */
export function HomePage() {
  const { data: me } = useMe();
  const { data: calendars = [] } = useCalendars();
  const today = fmt(dayjs());
  const { data: days } = useDays(today, today);
  const { data: notifications } = useNotifications();
  const todayMap = new Map<number, ResolvedDay>((days ?? []).map((c) => [c.calendarId, c.days[0]]));

  return (
    <Box>
      <PageHeader title={`ようこそ、${me?.displayName ?? ''} さん`} subtitle="IL共有マスターは、各システムで共有するマスタ情報を一元管理します。" />

      <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' } }}>
        <Paper sx={{ p: 2 }}>
          <Typography variant="h3" sx={{ mb: 1 }}>本日（{fmtJp(today)}）の稼働状況</Typography>
          <Stack spacing={0.5}>
            {flattenTree(buildTree(calendars)).map((n) => {
              const d = todayMap.get(n.calendar.id);
              return (
                <Box
                  key={n.calendar.id}
                  component={RouterLink}
                  to={`/calendars/${n.calendar.id}`}
                  sx={{ display: 'flex', alignItems: 'center', gap: 1, pl: n.depth * 2, py: 0.5, color: 'inherit', textDecoration: 'none', borderRadius: 1, '&:hover': { bgcolor: 'action.hover' } }}
                >
                  <CalendarMonthIcon fontSize="small" color="primary" />
                  <Typography sx={{ flex: 1 }}>{n.calendar.name}</Typography>
                  {d && <DayChip day={d} />}
                </Box>
              );
            })}
          </Stack>
        </Paper>

        <Paper sx={{ p: 2 }}>
          <Typography variant="h3" sx={{ mb: 1 }}>
            お知らせ {!!notifications?.unreadCount && <Chip size="small" color="error" label={`未読 ${notifications.unreadCount}`} />}
          </Typography>
          {!notifications?.items.length ? (
            <Typography variant="body2" color="text.secondary">通知はありません</Typography>
          ) : (
            <Stack spacing={1}>
              {notifications.items.slice(0, 5).map((n) => (
                <Box key={n.id} component={n.linkUrl ? RouterLink : 'div'} to={n.linkUrl ?? ''} sx={{ color: 'inherit', textDecoration: 'none' }}>
                  <Typography variant="body2" sx={{ fontWeight: n.isRead ? 400 : 700 }}>{n.title}</Typography>
                  <Typography variant="caption" color="text.secondary">{fmtDateTime(n.createdAt)}　{n.message}</Typography>
                </Box>
              ))}
            </Stack>
          )}
        </Paper>
      </Box>

      <Typography variant="h3" sx={{ mt: 3, mb: 1 }}>マスタ</Typography>
      <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr 1fr', sm: 'repeat(3, 1fr)', lg: 'repeat(5, 1fr)' } }}>
        {[{ label: 'カレンダー', path: '/calendars', icon: <CalendarMonthIcon />, dummy: false }, ...dummyMenus].map((m) => (
          <Card key={m.path} variant="outlined">
            <CardActionArea component={RouterLink} to={m.path} sx={{ height: '100%' }}>
              <CardContent sx={{ textAlign: 'center', color: m.dummy ? 'text.disabled' : 'primary.main' }}>
                <Box sx={{ '& svg': { fontSize: 36 } }}>{m.icon}</Box>
                <Typography sx={{ fontWeight: 700, color: 'text.primary' }}>{m.label}</Typography>
                {m.dummy && <Typography variant="caption">準備中</Typography>}
              </CardContent>
            </CardActionArea>
          </Card>
        ))}
      </Box>
    </Box>
  );
}
