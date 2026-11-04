import { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import ChevronLeftIcon from '@mui/icons-material/ChevronLeft';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import AddIcon from '@mui/icons-material/Add';
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import dayjs from 'dayjs';
import { useMe, usePublicHolidayMutations, usePublicHolidays } from '../../api/queries';
import { PageHeader } from '../../components/PageHeader';
import { useSnackbar } from '../../components/useSnackbar';
import { fmtJp } from '../../utils/date';

/** 祝日マスタ（全カレンダー共通） */
export function PublicHolidaysPage() {
  const [year, setYear] = useState(dayjs().year());
  const { data: me } = useMe();
  const { data: holidays = [] } = usePublicHolidays(year);
  const { generate, save, remove } = usePublicHolidayMutations();
  const [editing, setEditing] = useState<{ id?: number; date: string; name: string } | null>(null);
  const snackbar = useSnackbar();
  const isAdmin = !!me?.isSystemAdmin;

  return (
    <Box sx={{ maxWidth: 800 }}>
      <PageHeader
        title="祝日"
        subtitle="全カレンダー共通の祝日です。各カレンダーの設定で「祝日を稼働日として扱う」ことも可能です。"
        actions={
          isAdmin && (
            <>
              <Button
                variant="outlined"
                startIcon={<AutoFixHighIcon />}
                disabled={generate.isPending}
                onClick={async () => {
                  const r = await generate.mutateAsync(year);
                  snackbar.show(`${year}年の祝日を ${r.added} 件追加しました（既存 ${r.skipped} 件はスキップ）`);
                }}
              >
                {year}年を自動生成
              </Button>
              <Button variant="contained" startIcon={<AddIcon />} onClick={() => setEditing({ date: `${year}-01-01`, name: '' })}>
                追加
              </Button>
            </>
          )
        }
      />
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
        <IconButton onClick={() => setYear(year - 1)} aria-label="前年"><ChevronLeftIcon /></IconButton>
        <Typography sx={{ fontWeight: 700, fontSize: 18, minWidth: 80, textAlign: 'center' }}>{year}年</Typography>
        <IconButton onClick={() => setYear(year + 1)} aria-label="翌年"><ChevronRightIcon /></IconButton>
        <Typography variant="body2" color="text.secondary" sx={{ ml: 2 }}>{holidays.length} 件</Typography>
      </Box>
      {generate.error && <Alert severity="error">{generate.error.message}</Alert>}
      {remove.error && <Alert severity="error">{remove.error.message}</Alert>}
      <Paper>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>日付</TableCell>
              <TableCell>名称</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {holidays.map((h) => (
              <TableRow key={h.id} hover>
                <TableCell sx={{ color: 'error.main', fontWeight: 600, whiteSpace: 'nowrap' }}>{fmtJp(h.date)}</TableCell>
                <TableCell>{h.name}</TableCell>
                <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                  {isAdmin && (
                    <>
                      <Tooltip title="編集">
                        <IconButton size="small" onClick={() => setEditing({ ...h })}><EditIcon fontSize="small" /></IconButton>
                      </Tooltip>
                      <Tooltip title="削除">
                        <IconButton size="small" onClick={() => remove.mutate(h.id)}><DeleteIcon fontSize="small" /></IconButton>
                      </Tooltip>
                    </>
                  )}
                </TableCell>
              </TableRow>
            ))}
            {holidays.length === 0 && (
              <TableRow>
                <TableCell colSpan={3} sx={{ textAlign: 'center', py: 4, color: 'text.secondary' }}>
                  祝日が登録されていません{isAdmin && '。「自動生成」で法律に基づく祝日を登録できます。'}
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </Paper>

      <Dialog open={!!editing} onClose={() => setEditing(null)} fullWidth maxWidth="xs">
        <DialogTitle>{editing?.id ? '祝日の編集' : '祝日の追加'}</DialogTitle>
        {editing && (
          <DialogContent>
            <Stack spacing={2} sx={{ pt: 1 }}>
              <TextField type="date" label="日付" value={editing.date} onChange={(e) => setEditing({ ...editing, date: e.target.value })} slotProps={{ inputLabel: { shrink: true } }} />
              <TextField label="名称" value={editing.name} onChange={(e) => setEditing({ ...editing, name: e.target.value })} />
              {save.error && <Alert severity="error" sx={{ whiteSpace: 'pre-line' }}>{save.error.message}</Alert>}
            </Stack>
          </DialogContent>
        )}
        <DialogActions>
          <Button onClick={() => setEditing(null)}>キャンセル</Button>
          <Button
            variant="contained"
            onClick={async () => {
              if (!editing) return;
              await save.mutateAsync({ id: editing.id, body: { date: editing.date, name: editing.name } });
              setEditing(null);
            }}
          >
            保存
          </Button>
        </DialogActions>
      </Dialog>
      {snackbar.element}
    </Box>
  );
}
