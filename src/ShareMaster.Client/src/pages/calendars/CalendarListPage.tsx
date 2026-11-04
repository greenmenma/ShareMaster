import { useMemo, useState } from 'react';
import { Link as RouterLink, useNavigate } from 'react-router';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  FormControlLabel,
  LinearProgress,
  ListItemText,
  MenuItem,
  Paper,
  Select,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableFooter,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import DownloadIcon from '@mui/icons-material/Download';
import StickyNote2Icon from '@mui/icons-material/StickyNote2';
import dayjs from 'dayjs';
import { useCalendars, useDays, useMe, useYearRange } from '../../api/queries';
import type { ResolvedDay } from '../../api/types';
import { PageHeader } from '../../components/PageHeader';
import { ViewUnitBar } from '../../components/ViewUnitBar';
import { DayLegend } from '../../components/DayLegend';
import { DayEditDialog } from '../../components/DayEditDialog';
import { useSnackbar } from '../../components/useSnackbar';
import { WEEKDAYS, daysBetween, fmt } from '../../utils/date';
import { COLORS, dayVisual } from '../../utils/dayStyle';
import { buildTree, flattenTree, lineage } from '../../utils/calendarTree';
import { useViewState, viewRange } from '../../utils/viewUnit';
import { downloadCsv } from '../../utils/csv';

/** 通常の土日以外の「特別な日」か（祝日・個別設定・特記事項・土日以外の定休日など） */
function isSpecial(d: ResolvedDay | undefined, dow: number) {
  if (!d) return false;
  if (d.notes.length > 0 || d.source === 'Override' || d.source === 'PublicHoliday') return true;
  if (d.source === 'Weekly') return dow !== 0 && dow !== 6;
  return dow === 0 || dow === 6; // 土日なのに稼働日（定休曜日の違い）
}

/** カレンダー一覧：行＝日付、列＝カレンダーのデータグリッドで各カレンダーの休みを比較する */
export function CalendarListPage() {
  const view = useViewState('month');
  const { data: range } = useYearRange();
  const { data: me } = useMe();
  const { data: calendars = [] } = useCalendars();
  const { from, to } = viewRange(view, range);
  const { data: days, isFetching, error } = useDays(fmt(from), fmt(to));
  const navigate = useNavigate();
  const snackbar = useSnackbar();

  const ordered = useMemo(() => flattenTree(buildTree(calendars)), [calendars]);
  const [hidden, setHidden] = useState<number[]>([]);
  const [specialOnlyByUnit, setSpecialOnlyByUnit] = useState({ all: true, year: true, month: false });
  const specialOnly = specialOnlyByUnit[view.unit];
  const [editing, setEditing] = useState<{ calendarId: number; date: string } | null>(null);

  const columns = ordered.filter((n) => !hidden.includes(n.calendar.id));
  const dayMap = useMemo(() => {
    const m = new Map<number, Map<string, ResolvedDay>>();
    for (const c of days ?? []) m.set(c.calendarId, new Map(c.days.map((d) => [d.date, d])));
    return m;
  }, [days]);

  const allDates = daysBetween(from, to);
  const rows = specialOnly
    ? allDates.filter((d) => columns.some((c) => isSpecial(dayMap.get(c.calendar.id)?.get(fmt(d)), d.day())))
    : allDates;

  const holidayCount = (calendarId: number) => {
    const m = dayMap.get(calendarId);
    let n = 0;
    m?.forEach((d) => d.isHoliday && n++);
    return n;
  };

  const exportCsv = () => {
    const header = ['日付', '曜日', ...columns.map((c) => c.calendar.name)];
    const body = rows.map((d) => [
      fmt(d),
      WEEKDAYS[d.day()],
      ...columns.map((c) => {
        const x = dayMap.get(c.calendar.id)?.get(fmt(d));
        if (!x) return '';
        const kind = x.isHoliday ? `休:${x.name ?? x.dayTypeName ?? ''}` : x.source === 'Override' ? `稼:${x.dayTypeName ?? ''}` : '';
        const notes = x.notes.map((n) => n.text).join(' / ');
        return [kind, notes].filter(Boolean).join(' ');
      }),
    ]);
    downloadCsv(`カレンダー一覧_${fmt(from)}_${fmt(to)}.csv`, [header, ...body]);
  };

  return (
    <Box>
      <PageHeader
        title="カレンダー一覧"
        subtitle="各カレンダーの休日・特記事項を比較できます。セルをクリックすると編集画面を開きます。"
        actions={
          <>
            <Button startIcon={<DownloadIcon />} variant="outlined" onClick={exportCsv}>
              CSV
            </Button>
            {me?.isSystemAdmin && (
              <Button startIcon={<AddIcon />} variant="contained" component={RouterLink} to="/calendars/new">
                カレンダー追加
              </Button>
            )}
          </>
        }
      />

      <ViewUnitBar view={view} range={range}>
        <FormControlLabel
          control={
            <Checkbox
              checked={specialOnly}
              onChange={(e) => setSpecialOnlyByUnit({ ...specialOnlyByUnit, [view.unit]: e.target.checked })}
            />
          }
          label="特別な日のみ（通常の土日を除く）"
        />
        <Select
          multiple
          size="small"
          displayEmpty
          value={ordered.map((n) => n.calendar.id).filter((id) => !hidden.includes(id))}
          onChange={(e) => {
            const selected = e.target.value as number[];
            setHidden(ordered.map((n) => n.calendar.id).filter((id) => !selected.includes(id)));
          }}
          renderValue={(v) => `表示カレンダー：${v.length}/${ordered.length}`}
          sx={{ minWidth: 200 }}
        >
          {ordered.map((n) => (
            <MenuItem key={n.calendar.id} value={n.calendar.id} sx={{ pl: 2 + n.depth * 2 }}>
              <Checkbox size="small" checked={!hidden.includes(n.calendar.id)} />
              <ListItemText primary={n.calendar.name} />
            </MenuItem>
          ))}
        </Select>
      </ViewUnitBar>

      <Box sx={{ mb: 1 }}>
        <DayLegend />
      </Box>

      {error && <Alert severity="error">{error.message}</Alert>}

      <Paper sx={{ position: 'relative' }}>
        {isFetching && <LinearProgress sx={{ position: 'absolute', top: 0, left: 0, right: 0, zIndex: 3 }} />}
        <TableContainer sx={{ maxHeight: { xs: 'calc(100vh - 300px)', md: 'calc(100vh - 290px)' }, minHeight: 300 }}>
          <Table stickyHeader size="small" sx={{ '& td, & th': { borderRight: '1px solid #ECEAF4' } }}>
            <TableHead>
              <TableRow>
                <TableCell sx={{ left: 0, zIndex: 3, minWidth: 96, bgcolor: '#F1EFFA', fontWeight: 700 }}>日付</TableCell>
                {columns.map((n) => (
                  <TableCell key={n.calendar.id} sx={{ bgcolor: '#F1EFFA', minWidth: 110, p: 1, verticalAlign: 'bottom' }}>
                    <Tooltip title={lineage(calendars, n.calendar.id).map((c) => c.name).join(' > ')}>
                      <Box>
                        {n.depth > 0 && (
                          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', lineHeight: 1.2 }} noWrap>
                            {lineage(calendars, n.calendar.id).slice(-2, -1)[0]?.name} &gt;
                          </Typography>
                        )}
                        <Typography
                          component={RouterLink}
                          to={`/calendars/${n.calendar.id}?unit=${view.unit}&y=${view.year}&m=${view.month}`}
                          sx={{ fontWeight: 700, fontSize: 14, color: 'primary.dark', textDecoration: 'none', '&:hover': { textDecoration: 'underline' } }}
                        >
                          {n.calendar.name}
                        </Typography>
                      </Box>
                    </Tooltip>
                  </TableCell>
                ))}
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.map((d) => {
                const key = fmt(d);
                const dow = d.day();
                const isToday = key === fmt(dayjs());
                return (
                  <TableRow key={key} hover>
                    <TableCell
                      sx={{
                        position: 'sticky',
                        left: 0,
                        zIndex: 1,
                        bgcolor: isToday ? '#E6E3F7' : '#FBFAFF',
                        whiteSpace: 'nowrap',
                        fontWeight: 600,
                        color: dow === 0 ? COLORS.sunday : dow === 6 ? COLORS.saturday : undefined,
                      }}
                    >
                      {view.unit === 'all' ? d.format('YYYY/MM/DD') : d.format('MM/DD')}（{WEEKDAYS[dow]}）
                    </TableCell>
                    {columns.map((n) => {
                      const x = dayMap.get(n.calendar.id)?.get(key);
                      const v = dayVisual(x, dow);
                      const tip = [
                        x?.isHoliday ? '休日' : '稼働日',
                        x?.name ?? x?.dayTypeName,
                        x?.sourceCalendarName && !x.hasOwnSetting ? `（${x.sourceCalendarName}の設定を継承）` : null,
                        ...(x?.notes ?? []).map((nn) => `📝 ${nn.calendarName}：${nn.text}`),
                      ]
                        .filter(Boolean)
                        .join('\n');
                      return (
                        <TableCell
                          key={n.calendar.id}
                          onClick={() => setEditing({ calendarId: n.calendar.id, date: key })}
                          sx={{
                            bgcolor: v.background,
                            cursor: 'pointer',
                            p: 0.75,
                            '&:hover': { boxShadow: 'inset 0 0 0 2px rgba(81,72,157,0.5)' },
                          }}
                        >
                          <Tooltip title={<span style={{ whiteSpace: 'pre-line' }}>{tip}</span>} disableInteractive>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, minHeight: 22 }}>
                              {x?.hasOwnSetting && <Box sx={{ width: 6, height: 6, borderRadius: '50%', bgcolor: 'primary.main', flexShrink: 0 }} />}
                              <Typography
                                noWrap
                                sx={{ fontSize: 12, fontWeight: 600, color: v.accent ?? 'text.secondary', flex: 1 }}
                              >
                                {x?.isHoliday ? (v.label ?? '休') : x?.source === 'Override' ? (x.dayTypeName ?? '') : ''}
                              </Typography>
                              {(x?.notes.length ?? 0) > 0 && <StickyNote2Icon sx={{ fontSize: 14, color: 'secondary.main' }} />}
                            </Box>
                          </Tooltip>
                        </TableCell>
                      );
                    })}
                  </TableRow>
                );
              })}
              {rows.length === 0 && (
                <TableRow>
                  <TableCell colSpan={columns.length + 1} sx={{ textAlign: 'center', py: 4, color: 'text.secondary' }}>
                    表示する日付がありません
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
            <TableFooter>
              <TableRow sx={{ '& td': { position: 'sticky', bottom: 0, bgcolor: '#F1EFFA', fontWeight: 700, color: 'text.primary' } }}>
                <TableCell sx={{ left: 0, zIndex: 3 }}>休日数</TableCell>
                {columns.map((n) => (
                  <TableCell key={n.calendar.id}>{holidayCount(n.calendar.id)} 日</TableCell>
                ))}
              </TableRow>
            </TableFooter>
          </Table>
        </TableContainer>
      </Paper>

      <DayEditDialog
        calendarId={editing?.calendarId ?? 0}
        date={editing?.date ?? null}
        onClose={() => setEditing(null)}
        onSaved={(m) => snackbar.show(m)}
      />
      {snackbar.element}
      {calendars.length === 0 && !isFetching && (
        <Alert severity="info" sx={{ mt: 2 }} action={me?.isSystemAdmin && <Button onClick={() => navigate('/calendars/new')}>追加</Button>}>
          カレンダーが登録されていません。
        </Alert>
      )}
    </Box>
  );
}
