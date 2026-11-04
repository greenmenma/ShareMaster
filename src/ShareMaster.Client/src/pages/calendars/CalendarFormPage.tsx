import { useEffect, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  FormGroup,
  IconButton,
  LinearProgress,
  MenuItem,
  Paper,
  Stack,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import { useCalendars, useDeleteCalendar, useMe, useSaveCalendar } from '../../api/queries';
import type { CalendarSaveRequest, Manager } from '../../api/types';
import { PageHeader } from '../../components/PageHeader';
import { UserPicker } from '../../components/UserPicker';
import { WEEKDAYS } from '../../utils/date';
import { buildTree, descendantIds, flattenTree } from '../../utils/calendarTree';

const empty: CalendarSaveRequest = {
  code: '',
  name: '',
  description: '',
  parentId: null,
  sortOrder: 0,
  weeklyHolidays: null,
  usePublicHolidays: null,
  managers: [],
};

/** カレンダーの新規追加（システム管理者）・設定変更（管理者） */
export function CalendarFormPage() {
  const params = useParams();
  const [sp] = useSearchParams();
  const id = params.id ? Number(params.id) : undefined;
  const isNew = !id;
  const navigate = useNavigate();
  const { data: me } = useMe();
  const { data: calendars = [], isLoading } = useCalendars();
  const save = useSaveCalendar();
  const remove = useDeleteCalendar();
  const [form, setForm] = useState<CalendarSaveRequest>(empty);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const current = calendars.find((c) => c.id === id);
  const isAdmin = !!me?.isSystemAdmin;

  useEffect(() => {
    if (isNew) {
      const parentId = Number(sp.get('parentId')) || null;
      const siblings = calendars.filter((c) => (c.parentId ?? null) === parentId);
      setForm({ ...empty, parentId, sortOrder: Math.max(0, ...siblings.map((c) => c.sortOrder)) + 10 });
    } else if (current) {
      setForm({
        code: current.code,
        name: current.name,
        description: current.description ?? '',
        parentId: current.parentId ?? null,
        sortOrder: current.sortOrder,
        weeklyHolidays: current.weeklyHolidays == null ? null : current.weeklyHolidays.split(',').filter(Boolean).map(Number),
        usePublicHolidays: current.usePublicHolidays ?? null,
        managers: current.managers,
      });
    }
  }, [isNew, current?.id, current?.updatedAt, calendars.length]); // eslint-disable-line

  if (isLoading) return <LinearProgress />;
  if (isNew && me && !isAdmin) return <Alert severity="warning">カレンダーの新規追加はシステム管理者のみ可能です。</Alert>;
  if (!isNew && !current) return <Alert severity="warning">カレンダーが見つかりません。</Alert>;
  if (current && !current.canEdit) return <Alert severity="warning">このカレンダーを編集する権限がありません。</Alert>;

  // 自分自身と子孫は親に選べない
  const excluded = id ? descendantIds(calendars, id) : new Set<number>();
  const parentOptions = flattenTree(buildTree(calendars)).filter((n) => !excluded.has(n.calendar.id));
  const parent = calendars.find((c) => c.id === form.parentId);
  const inheritWeekly = form.weeklyHolidays == null;

  const set = <K extends keyof CalendarSaveRequest>(k: K, v: CalendarSaveRequest[K]) => setForm((f) => ({ ...f, [k]: v }));
  const setManager = (key: string, patch: Partial<Manager>) =>
    set('managers', form.managers.map((m) => (m.userKey === key ? { ...m, ...patch } : m)));

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const saved = await save.mutateAsync({ id, body: form });
    navigate(`/calendars/${saved.id}`);
  };

  return (
    <Box component="form" onSubmit={submit} sx={{ maxWidth: 880 }}>
      <PageHeader title={isNew ? 'カレンダー追加' : `${current?.name} の設定`} />
      <Stack spacing={2}>
        <Paper sx={{ p: 2 }}>
          <Typography variant="h3" sx={{ mb: 2 }}>基本情報</Typography>
          <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' } }}>
            <TextField
              label="コード"
              required
              value={form.code}
              onChange={(e) => set('code', e.target.value)}
              disabled={!isAdmin}
              helperText="英数字・ハイフン・アンダースコア（他システム連携用）"
              slotProps={{ htmlInput: { maxLength: 50, pattern: '[A-Za-z0-9_\\-]+' } }}
            />
            <TextField label="名称" required value={form.name} onChange={(e) => set('name', e.target.value)} slotProps={{ htmlInput: { maxLength: 100 } }} />
            <TextField
              select
              label="親カレンダー"
              value={form.parentId ?? ''}
              onChange={(e) => set('parentId', e.target.value === '' ? null : Number(e.target.value))}
              disabled={!isAdmin}
              helperText="親の休日設定を引き継ぎ、このカレンダーで上書きします"
            >
              <MenuItem value="">（なし：最上位）</MenuItem>
              {parentOptions.map((n) => (
                <MenuItem key={n.calendar.id} value={n.calendar.id} sx={{ pl: 2 + n.depth * 2 }}>
                  {n.calendar.name}
                </MenuItem>
              ))}
            </TextField>
            <TextField type="number" label="表示順" value={form.sortOrder} onChange={(e) => set('sortOrder', Number(e.target.value))} />
            <TextField
              label="説明"
              value={form.description ?? ''}
              onChange={(e) => set('description', e.target.value)}
              multiline
              minRows={2}
              sx={{ gridColumn: { sm: '1 / -1' } }}
              slotProps={{ htmlInput: { maxLength: 1000 } }}
            />
          </Box>
        </Paper>

        <Paper sx={{ p: 2 }}>
          <Typography variant="h3" sx={{ mb: 1 }}>休日の基本ルール</Typography>
          <FormControlLabel
            control={
              <Switch
                checked={inheritWeekly}
                onChange={(e) => set('weeklyHolidays', e.target.checked ? null : (parent?.effectiveWeeklyHolidays ?? [0, 6]))}
              />
            }
            label={parent ? `定休曜日は親（${parent.name}）を継承` : '定休曜日は既定（土・日）を使用'}
          />
          <FormGroup row sx={{ pl: 1, mb: 1 }}>
            {WEEKDAYS.map((w, i) => {
              const effective = inheritWeekly ? (parent?.effectiveWeeklyHolidays ?? [0, 6]) : (form.weeklyHolidays ?? []);
              return (
                <FormControlLabel
                  key={w}
                  disabled={inheritWeekly}
                  control={
                    <Checkbox
                      checked={effective.includes(i)}
                      onChange={(e) =>
                        set('weeklyHolidays', e.target.checked ? [...(form.weeklyHolidays ?? []), i] : (form.weeklyHolidays ?? []).filter((x) => x !== i))
                      }
                    />
                  }
                  label={w}
                />
              );
            })}
          </FormGroup>
          <TextField
            select
            label="祝日の扱い"
            value={form.usePublicHolidays == null ? 'inherit' : form.usePublicHolidays ? 'holiday' : 'workday'}
            onChange={(e) => set('usePublicHolidays', e.target.value === 'inherit' ? null : e.target.value === 'holiday')}
            sx={{ minWidth: 280 }}
          >
            <MenuItem value="inherit">{parent ? `親を継承（${parent.effectiveUsePublicHolidays ? '休日' : '稼働日'}）` : '既定（休日）'}</MenuItem>
            <MenuItem value="holiday">休日として扱う</MenuItem>
            <MenuItem value="workday">稼働日として扱う</MenuItem>
          </TextField>
        </Paper>

        <Paper sx={{ p: 2 }}>
          <Typography variant="h3" sx={{ mb: 0.5 }}>管理者</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
            管理者はこのカレンダーと子孫カレンダーを編集できます。親カレンダーが更新されると通知を受け取ります。
          </Typography>
          <UserPicker
            label="管理者を追加"
            excludeKeys={form.managers.map((m) => m.userKey)}
            onSelect={(u) => set('managers', [...form.managers, { userKey: u.userKey, displayName: u.displayName, receiveNotifications: true }])}
          />
          {form.managers.length > 0 && (
            <Table size="small" sx={{ mt: 1 }}>
              <TableHead>
                <TableRow>
                  <TableCell>氏名</TableCell>
                  <TableCell sx={{ display: { xs: 'none', sm: 'table-cell' } }}>ユーザー</TableCell>
                  <TableCell>通知</TableCell>
                  <TableCell />
                </TableRow>
              </TableHead>
              <TableBody>
                {form.managers.map((m) => (
                  <TableRow key={m.userKey}>
                    <TableCell>{m.displayName}</TableCell>
                    <TableCell sx={{ display: { xs: 'none', sm: 'table-cell' } }}>{m.userKey}</TableCell>
                    <TableCell>
                      <Switch size="small" checked={m.receiveNotifications} onChange={(e) => setManager(m.userKey, { receiveNotifications: e.target.checked })} />
                    </TableCell>
                    <TableCell align="right">
                      <Tooltip title="削除">
                        <IconButton size="small" onClick={() => set('managers', form.managers.filter((x) => x.userKey !== m.userKey))}>
                          <DeleteIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </Paper>

        {save.error && <Alert severity="error" sx={{ whiteSpace: 'pre-line' }}>{save.error.message}</Alert>}
        {remove.error && <Alert severity="error">{remove.error.message}</Alert>}

        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
          <Button type="submit" variant="contained" disabled={save.isPending}>
            保存
          </Button>
          <Button onClick={() => navigate(-1)}>キャンセル</Button>
          <Box sx={{ flex: 1 }} />
          {!isNew && isAdmin && (
            <Button color="error" startIcon={<DeleteIcon />} onClick={() => setConfirmDelete(true)}>
              削除
            </Button>
          )}
        </Box>
      </Stack>

      <Dialog open={confirmDelete} onClose={() => setConfirmDelete(false)}>
        <DialogTitle>カレンダーの削除</DialogTitle>
        <DialogContent>
          「{current?.name}」を削除します。変更履歴は保持されます。よろしいですか？
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirmDelete(false)}>キャンセル</Button>
          <Button
            color="error"
            variant="contained"
            onClick={async () => {
              setConfirmDelete(false);
              await remove.mutateAsync(id!);
              navigate('/calendars');
            }}
          >
            削除
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
