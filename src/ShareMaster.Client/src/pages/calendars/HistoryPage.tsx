import { Fragment, useState } from 'react';
import { useSearchParams } from 'react-router';
import {
  Box,
  Chip,
  Collapse,
  IconButton,
  MenuItem,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import KeyboardArrowDownIcon from '@mui/icons-material/KeyboardArrowDown';
import KeyboardArrowUpIcon from '@mui/icons-material/KeyboardArrowUp';
import { useCalendars, useHistories } from '../../api/queries';
import type { History } from '../../api/types';
import { PageHeader } from '../../components/PageHeader';
import { buildTree, flattenTree } from '../../utils/calendarTree';
import { fmtDateTime } from '../../utils/date';

const ENTITY_LABELS: Record<string, string> = {
  Calendar: 'カレンダー設定',
  CalendarDay: '日別設定',
  DayType: '日種別',
  PublicHoliday: '祝日',
  SystemAdmin: 'システム管理者',
};

/** 履歴 JSON の項目名の表示名 */
const FIELD_LABELS: Record<string, string> = {
  code: 'コード',
  name: '名称',
  description: '説明',
  parentId: '親カレンダーID',
  sortOrder: '表示順',
  weeklyHolidays: '定休曜日',
  usePublicHolidays: '祝日の扱い',
  managers: '管理者',
  date: '日付',
  dayType: '日種別',
  note: '特記事項',
  color: '表示色',
  isHoliday: '休日',
  userKey: 'ユーザー',
  displayName: '氏名',
  id: 'ID',
  isSystem: 'システム予約',
};

const ACTION_COLORS = { Create: 'success', Update: 'info', Delete: 'error' } as const;
const ACTION_LABELS = { Create: '追加', Update: '変更', Delete: '削除' } as const;

/** 変更履歴の一覧 */
export function HistoryPage() {
  const [sp, setSp] = useSearchParams();
  const calendarId = Number(sp.get('calendarId')) || undefined;
  const [entityType, setEntityType] = useState('');
  const [keyword, setKeyword] = useState('');
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(50);
  const { data: calendars = [] } = useCalendars();
  const { data } = useHistories({ calendarId, entityType: entityType || undefined, keyword: keyword || undefined, page: page + 1, pageSize });

  return (
    <Box>
      <PageHeader title="変更履歴" subtitle="カレンダー・日別設定・マスタの変更内容を確認できます。" />
      <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mb: 2 }}>
        <TextField
          select
          size="small"
          label="カレンダー"
          value={calendarId ?? ''}
          onChange={(e) => {
            setPage(0);
            setSp(e.target.value ? { calendarId: e.target.value } : {});
          }}
          sx={{ minWidth: 200 }}
        >
          <MenuItem value="">すべて</MenuItem>
          {flattenTree(buildTree(calendars)).map((n) => (
            <MenuItem key={n.calendar.id} value={n.calendar.id} sx={{ pl: 2 + n.depth * 2 }}>
              {n.calendar.name}
            </MenuItem>
          ))}
        </TextField>
        <TextField select size="small" label="対象" value={entityType} onChange={(e) => { setPage(0); setEntityType(e.target.value); }} sx={{ minWidth: 160 }}>
          <MenuItem value="">すべて</MenuItem>
          {Object.entries(ENTITY_LABELS).map(([k, v]) => (
            <MenuItem key={k} value={k}>{v}</MenuItem>
          ))}
        </TextField>
        <TextField size="small" label="キーワード（内容・更新者）" value={keyword} onChange={(e) => { setPage(0); setKeyword(e.target.value); }} sx={{ minWidth: 240 }} />
      </Box>
      <Paper>
        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell width={40} />
                <TableCell>日時</TableCell>
                <TableCell>操作</TableCell>
                <TableCell sx={{ display: { xs: 'none', md: 'table-cell' } }}>カレンダー</TableCell>
                <TableCell>内容</TableCell>
                <TableCell sx={{ display: { xs: 'none', sm: 'table-cell' } }}>更新者</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data?.items.map((h) => <HistoryRow key={h.id} h={h} />)}
              {data?.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} sx={{ textAlign: 'center', py: 4, color: 'text.secondary' }}>履歴はありません</TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </Box>
        <TablePagination
          component="div"
          count={data?.totalCount ?? 0}
          page={page}
          onPageChange={(_, p) => setPage(p)}
          rowsPerPage={pageSize}
          onRowsPerPageChange={(e) => { setPageSize(Number(e.target.value)); setPage(0); }}
          rowsPerPageOptions={[20, 50, 100]}
          labelRowsPerPage="表示件数"
          labelDisplayedRows={({ from, to, count }) => `${from}–${to} / ${count}件`}
        />
      </Paper>
    </Box>
  );
}

function HistoryRow({ h }: { h: History }) {
  const [open, setOpen] = useState(false);
  const before = h.beforeJson ? (JSON.parse(h.beforeJson) as Record<string, unknown>) : null;
  const after = h.afterJson ? (JSON.parse(h.afterJson) as Record<string, unknown>) : null;
  const keys = [...new Set([...Object.keys(before ?? {}), ...Object.keys(after ?? {})])];
  const show = (v: unknown) => (v === null || v === undefined || v === '' ? '—' : Array.isArray(v) ? v.join('、') : String(v));

  return (
    <Fragment>
      <TableRow hover sx={{ '& > td': { borderBottom: open ? 'none' : undefined } }}>
        <TableCell>
          {keys.length > 0 && (
            <IconButton size="small" onClick={() => setOpen(!open)} aria-label="詳細">
              {open ? <KeyboardArrowUpIcon /> : <KeyboardArrowDownIcon />}
            </IconButton>
          )}
        </TableCell>
        <TableCell sx={{ whiteSpace: 'nowrap' }}>{fmtDateTime(h.changedAt)}</TableCell>
        <TableCell>
          <Chip size="small" color={ACTION_COLORS[h.action]} variant="outlined" label={`${ENTITY_LABELS[h.entityType] ?? h.entityType} ${ACTION_LABELS[h.action]}`} />
        </TableCell>
        <TableCell sx={{ display: { xs: 'none', md: 'table-cell' } }}>{h.calendarName ?? '—'}</TableCell>
        <TableCell>{h.summary}</TableCell>
        <TableCell sx={{ display: { xs: 'none', sm: 'table-cell' }, whiteSpace: 'nowrap' }}>{h.changedByName}</TableCell>
      </TableRow>
      <TableRow>
        <TableCell colSpan={6} sx={{ py: 0 }}>
          <Collapse in={open} unmountOnExit>
            <Table size="small" sx={{ my: 1, maxWidth: 720, '& td, & th': { fontSize: 13 } }}>
              <TableHead>
                <TableRow>
                  <TableCell>項目</TableCell>
                  <TableCell>変更前</TableCell>
                  <TableCell>変更後</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {keys.map((k) => {
                  const b = show(before?.[k]);
                  const a = show(after?.[k]);
                  return (
                    <TableRow key={k} sx={{ bgcolor: b !== a ? 'rgba(224,160,48,0.08)' : undefined }}>
                      <TableCell>{FIELD_LABELS[k] ?? k}</TableCell>
                      <TableCell><Typography variant="body2" color="text.secondary">{b}</Typography></TableCell>
                      <TableCell><Typography variant="body2" sx={{ fontWeight: b !== a ? 700 : 400 }}>{a}</Typography></TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </Collapse>
        </TableCell>
      </TableRow>
    </Fragment>
  );
}
