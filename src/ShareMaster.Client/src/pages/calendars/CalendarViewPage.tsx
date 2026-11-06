import { useMemo, useState } from 'react';
import { Link as RouterLink, useParams } from 'react-router';
import {
  Alert,
  Box,
  Breadcrumbs,
  Button,
  Chip,
  Divider,
  LinearProgress,
  Link,
  List,
  ListItemButton,
  ListItemText,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import SettingsIcon from '@mui/icons-material/Settings';
import HistoryIcon from '@mui/icons-material/History';
import AddIcon from '@mui/icons-material/Add';
import DownloadIcon from '@mui/icons-material/Download';
import type { Dayjs } from 'dayjs';
import { useCalendars, useDays, useMe, useYearRange } from '../../api/queries';
import type { ResolvedDay } from '../../api/types';
import { PageHeader } from '../../components/PageHeader';
import { FiscalPeriod } from '../../components/FiscalPeriod';
import { ViewUnitBar } from '../../components/ViewUnitBar';
import { MonthCalendar } from '../../components/MonthCalendar';
import { YearCalendar } from '../../components/YearCalendar';
import { DayLegend } from '../../components/DayLegend';
import { DayEditDialog } from '../../components/DayEditDialog';
import { DayChip } from '../../components/DayChip';
import { useSnackbar } from '../../components/useSnackbar';
import { WEEKDAYS, fmt, fmtJp } from '../../utils/date';
import { lineage } from '../../utils/calendarTree';
import { useViewState, viewRange } from '../../utils/viewUnit';
import { downloadCsv } from '../../utils/csv';
import { isNotable } from '../../utils/dayStyle';

/** カレンダー表示（すべて／年／月）。日付クリックで編集 */
export function CalendarViewPage() {
  const id = Number(useParams().id);
  const view = useViewState('month');
  const { data: me } = useMe();
  const { data: calendars = [], isLoading: calLoading } = useCalendars();
  const { data: range } = useYearRange();
  const { from, to } = viewRange(view, range);
  const { data, isFetching, error } = useDays(fmt(from), fmt(to), [id]);
  const [editing, setEditing] = useState<string | null>(null);
  const snackbar = useSnackbar();

  const calendar = calendars.find((c) => c.id === id);
  const path = lineage(calendars, id);
  const children = calendars.filter((c) => c.parentId === id);
  const days = useMemo(() => new Map((data?.[0]?.days ?? []).map((d) => [d.date, d] as [string, ResolvedDay])), [data]);

  const stats = useMemo(() => {
    let holidays = 0;
    let workdays = 0;
    days.forEach((d) => (d.isHoliday ? holidays++ : workdays++));
    return { holidays, workdays };
  }, [days]);

  const notableDays = useMemo(() => [...days.values()].filter((d) => isNotable(d) && (d.source !== 'Weekly' || d.notes.length > 0)), [days]);

  if (!calLoading && !calendar) return <Alert severity="warning">カレンダーが見つかりません。</Alert>;
  if (!calendar) return <LinearProgress />;

  const onDayClick = (d: Dayjs) => setEditing(fmt(d));
  const years = range ? Array.from({ length: range.maxYear - range.minYear + 1 }, (_, i) => range.minYear + i) : [];

  const exportCsv = () => {
    const rows = [...days.values()].map((d) => [
      d.date,
      WEEKDAYS[new Date(d.date).getDay()],
      d.isHoliday ? '休日' : '稼働日',
      d.dayTypeName ?? '',
      d.name ?? '',
      d.notes.map((n) => `${n.calendarName}:${n.text}`).join(' / '),
      d.sourceCalendarName ?? '',
    ]);
    downloadCsv(`${calendar.name}_${fmt(from)}_${fmt(to)}.csv`, [['日付', '曜日', '区分', '日種別', '名称', '特記事項', '設定元'], ...rows]);
  };

  return (
    <Box>
      <Breadcrumbs sx={{ mb: 1, fontSize: 14 }}>
        <Link component={RouterLink} to="/calendars" underline="hover" color="inherit">
          カレンダー
        </Link>
        {path.map((c) =>
          c.id === id ? (
            <Typography key={c.id} color="text.primary" sx={{ fontSize: 14 }}>
              {c.name}
            </Typography>
          ) : (
            <Link key={c.id} component={RouterLink} to={`/calendars/${c.id}`} underline="hover" color="inherit">
              {c.name}
            </Link>
          ),
        )}
      </Breadcrumbs>
      <PageHeader
        title={calendar.name}
        subtitle={calendar.description}
        actions={
          <>
            <Button startIcon={<DownloadIcon />} variant="outlined" onClick={exportCsv}>CSV</Button>
            <Button startIcon={<HistoryIcon />} variant="outlined" component={RouterLink} to={`/calendars/history?calendarId=${id}`}>
              変更履歴
            </Button>
            {me?.isSystemAdmin && (
              <Button startIcon={<AddIcon />} variant="outlined" component={RouterLink} to={`/calendars/new?parentId=${id}`}>
                子カレンダー
              </Button>
            )}
            {calendar.canEdit && (
              <Button startIcon={<SettingsIcon />} variant="contained" component={RouterLink} to={`/calendars/${id}/settings`}>
                設定
              </Button>
            )}
          </>
        }
      />

      <ViewUnitBar view={view} range={range} />
      {error && <Alert severity="error">{error.message}</Alert>}

      <Box sx={{ display: 'flex', gap: 2, flexDirection: { xs: 'column', lg: 'row' }, alignItems: 'flex-start' }}>
        <Box sx={{ flex: 1, minWidth: 0, width: '100%', position: 'relative' }}>
          {isFetching && <LinearProgress sx={{ position: 'absolute', top: -6, left: 0, right: 0 }} />}
          {view.unit === 'month' && <MonthCalendar year={view.year} month={view.month} days={days} onDayClick={onDayClick} />}
          {view.unit === 'year' && (
            <YearCalendar year={view.year} days={days} onDayClick={onDayClick} onMonthClick={(m) => view.set({ unit: 'month', month: m })} />
          )}
          {view.unit === 'all' && (
            <Stack spacing={3}>
              {years.map((y) => (
                <Box key={y}>
                  <Typography variant="h2" sx={{ mb: 1, cursor: 'pointer' }} onClick={() => view.set({ unit: 'year', year: y })}>
                    {y}年
                    <FiscalPeriod year={y} />
                  </Typography>
                  <YearCalendar
                    year={y}
                    days={days}
                    onDayClick={onDayClick}
                    onMonthClick={(m) => view.set({ unit: 'month', year: y, month: m })}
                  />
                </Box>
              ))}
            </Stack>
          )}
          <Box sx={{ mt: 1.5 }}>
            <DayLegend />
          </Box>
        </Box>

        <Stack spacing={2} sx={{ width: { xs: '100%', lg: 320 }, flexShrink: 0 }}>
          <Paper sx={{ p: 2 }}>
            <Typography variant="h3" sx={{ mb: 1 }}>表示期間の集計</Typography>
            <Box sx={{ display: 'flex', gap: 3 }}>
              <Box>
                <Typography variant="caption" color="text.secondary">休日</Typography>
                <Typography sx={{ fontSize: 24, fontWeight: 700, color: 'error.main' }}>{stats.holidays}<small>日</small></Typography>
              </Box>
              <Box>
                <Typography variant="caption" color="text.secondary">稼働日</Typography>
                <Typography sx={{ fontSize: 24, fontWeight: 700, color: 'primary.main' }}>{stats.workdays}<small>日</small></Typography>
              </Box>
            </Box>
          </Paper>

          <Paper sx={{ p: 2 }}>
            <Typography variant="h3" sx={{ mb: 1 }}>カレンダー情報</Typography>
            <Stack spacing={1} sx={{ fontSize: 14 }}>
              <Info label="コード" value={calendar.code} />
              <Info
                label="定休曜日"
                value={
                  <>
                    {calendar.effectiveWeeklyHolidays.map((d) => WEEKDAYS[d]).join('・') || 'なし'}
                    {calendar.weeklyHolidays == null && calendar.parentId && <Chip label="継承" size="small" sx={{ ml: 1, height: 18 }} />}
                  </>
                }
              />
              <Info
                label="祝日"
                value={
                  <>
                    {calendar.effectiveUsePublicHolidays ? '休日' : '稼働日'}
                    {calendar.usePublicHolidays == null && calendar.parentId && <Chip label="継承" size="small" sx={{ ml: 1, height: 18 }} />}
                  </>
                }
              />
              <Info label="管理者" value={calendar.managers.map((m) => m.displayName).join('、') || '未設定'} />
              {children.length > 0 && (
                <Info
                  label="子カレンダー"
                  value={children.map((c) => (
                    <Link key={c.id} component={RouterLink} to={`/calendars/${c.id}`} sx={{ mr: 1 }}>
                      {c.name}
                    </Link>
                  ))}
                />
              )}
            </Stack>
          </Paper>

          <Paper sx={{ p: 2 }}>
            <Typography variant="h3" sx={{ mb: 1 }}>祝日・特記事項</Typography>
            {notableDays.length === 0 ? (
              <Typography variant="body2" color="text.secondary">この期間にはありません</Typography>
            ) : (
              <List dense disablePadding sx={{ maxHeight: 420, overflowY: 'auto' }}>
                {notableDays.map((d) => (
                  <Box key={d.date}>
                    <ListItemButton onClick={() => setEditing(d.date)} sx={{ px: 1, alignItems: 'flex-start' }}>
                      <ListItemText
                        primary={
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
                            <Typography variant="body2" sx={{ fontWeight: 600 }}>{fmtJp(d.date)}</Typography>
                            <DayChip day={d} />
                          </Box>
                        }
                        secondary={d.notes.map((n) => n.text).join(' / ') || undefined}
                      />
                    </ListItemButton>
                    <Divider component="li" />
                  </Box>
                ))}
              </List>
            )}
          </Paper>
        </Stack>
      </Box>

      <DayEditDialog calendarId={id} date={editing} onClose={() => setEditing(null)} onSaved={(m) => snackbar.show(m)} />
      {snackbar.element}
    </Box>
  );
}

function Info({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Box sx={{ display: 'flex', gap: 1 }}>
      <Typography variant="body2" color="text.secondary" sx={{ width: 88, flexShrink: 0 }}>
        {label}
      </Typography>
      <Typography variant="body2" component="div">{value}</Typography>
    </Box>
  );
}
