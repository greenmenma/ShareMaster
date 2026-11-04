import { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  IconButton,
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
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import { useDayTypes, useDeleteDayType, useMe, useSaveDayType } from '../../api/queries';
import type { DayType, DayTypeSaveRequest } from '../../api/types';
import { PageHeader } from '../../components/PageHeader';
import { COLORS, alpha } from '../../utils/dayStyle';

/** 日種別（休日の種類と表示色）のマスタ */
export function DayTypesPage() {
  const { data: me } = useMe();
  const { data: types = [] } = useDayTypes();
  const save = useSaveDayType();
  const remove = useDeleteDayType();
  const [editing, setEditing] = useState<(DayTypeSaveRequest & { id?: number; isSystem?: boolean }) | null>(null);
  const isAdmin = !!me?.isSystemAdmin;

  const open = (t?: DayType) =>
    setEditing(t ? { ...t } : { code: '', name: '', color: '#5C6BC0', isHoliday: true, sortOrder: (types.at(-1)?.sortOrder ?? 0) + 10 });

  return (
    <Box sx={{ maxWidth: 900 }}>
      <PageHeader
        title="日種別・表示色"
        subtitle="カレンダーの日付に設定する休日の種類と表示色です。日曜・祝日（赤）、土曜（青）は固定色です。"
        actions={isAdmin && <Button variant="contained" startIcon={<AddIcon />} onClick={() => open()}>追加</Button>}
      />
      <Paper>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>表示</TableCell>
              <TableCell>コード</TableCell>
              <TableCell>区分</TableCell>
              <TableCell sx={{ display: { xs: 'none', sm: 'table-cell' } }}>表示順</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            <FixedRow label="日曜・祝日" color={COLORS.sunday} bg={COLORS.sundayBg} />
            <FixedRow label="土曜" color={COLORS.saturday} bg={COLORS.saturdayBg} />
            {types.map((t) => (
              <TableRow key={t.id} hover>
                <TableCell>
                  <Chip
                    label={t.name}
                    size="small"
                    sx={{ bgcolor: t.isHoliday ? alpha(t.color, 0.16) : '#fff', color: t.color, border: `1px solid ${t.color}`, fontWeight: 700 }}
                  />
                </TableCell>
                <TableCell>{t.code}</TableCell>
                <TableCell>{t.isHoliday ? '休日' : '稼働日'}</TableCell>
                <TableCell sx={{ display: { xs: 'none', sm: 'table-cell' } }}>{t.sortOrder}</TableCell>
                <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                  {isAdmin && (
                    <>
                      <Tooltip title="編集">
                        <IconButton size="small" onClick={() => open(t)}><EditIcon fontSize="small" /></IconButton>
                      </Tooltip>
                      {!t.isSystem && (
                        <Tooltip title="削除">
                          <IconButton size="small" onClick={() => remove.mutate(t.id)}><DeleteIcon fontSize="small" /></IconButton>
                        </Tooltip>
                      )}
                    </>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
      {remove.error && <Alert severity="error" sx={{ mt: 2 }}>{remove.error.message}</Alert>}

      <Dialog open={!!editing} onClose={() => setEditing(null)} fullWidth maxWidth="xs">
        <DialogTitle>{editing?.id ? '日種別の編集' : '日種別の追加'}</DialogTitle>
        {editing && (
          <DialogContent>
            <Stack spacing={2} sx={{ pt: 1 }}>
              <TextField label="コード" value={editing.code} disabled={editing.isSystem} onChange={(e) => setEditing({ ...editing, code: e.target.value })} />
              <TextField label="名称" value={editing.name} onChange={(e) => setEditing({ ...editing, name: e.target.value })} />
              <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
                <TextField
                  type="color"
                  label="表示色"
                  value={editing.color}
                  onChange={(e) => setEditing({ ...editing, color: e.target.value.toUpperCase() })}
                  sx={{ width: 120 }}
                />
                <Chip
                  label={editing.name || 'プレビュー'}
                  sx={{ bgcolor: editing.isHoliday ? alpha(editing.color, 0.16) : '#fff', color: editing.color, border: `1px solid ${editing.color}`, fontWeight: 700 }}
                />
              </Box>
              <FormControlLabel
                control={<Switch checked={editing.isHoliday} onChange={(e) => setEditing({ ...editing, isHoliday: e.target.checked })} />}
                label={editing.isHoliday ? '休日として扱う' : '稼働日として扱う'}
              />
              <TextField type="number" label="表示順" value={editing.sortOrder} onChange={(e) => setEditing({ ...editing, sortOrder: Number(e.target.value) })} />
              {save.error && <Alert severity="error" sx={{ whiteSpace: 'pre-line' }}>{save.error.message}</Alert>}
            </Stack>
          </DialogContent>
        )}
        <DialogActions>
          <Button onClick={() => setEditing(null)}>キャンセル</Button>
          <Button
            variant="contained"
            disabled={save.isPending}
            onClick={async () => {
              if (!editing) return;
              const { id, ...body } = editing;
              await save.mutateAsync({ id, body: { code: body.code, name: body.name, color: body.color, isHoliday: body.isHoliday, sortOrder: body.sortOrder } });
              setEditing(null);
            }}
          >
            保存
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}

function FixedRow({ label, color, bg }: { label: string; color: string; bg: string }) {
  return (
    <TableRow>
      <TableCell>
        <Chip label={label} size="small" sx={{ bgcolor: bg, color, border: `1px solid ${color}`, fontWeight: 700 }} />
      </TableCell>
      <TableCell colSpan={4}>
        <Box component="span" sx={{ color: 'text.secondary', fontSize: 13 }}>固定（定休曜日・祝日マスタから自動判定）</Box>
      </TableCell>
    </TableRow>
  );
}
