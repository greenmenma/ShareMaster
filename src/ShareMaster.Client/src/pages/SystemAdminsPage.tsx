import { useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
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
import DeleteIcon from '@mui/icons-material/Delete';
import DeleteSweepIcon from '@mui/icons-material/DeleteSweep';
import PlaylistAddIcon from '@mui/icons-material/PlaylistAdd';
import { useDataMaintenance, useMe, useSystemAdminMutations, useSystemAdmins } from '../api/queries';
import { useSnackbar } from '../components/useSnackbar';
import { PageHeader } from '../components/PageHeader';
import { UserPicker } from '../components/UserPicker';
import { fmtDateTime } from '../utils/date';

/** システム管理者の管理（カレンダーの新規追加・削除、マスタ管理が可能な利用者） */
export function SystemAdminsPage() {
  const { data: me } = useMe();
  const { data: admins = [] } = useSystemAdmins(!!me?.isSystemAdmin);
  const { add, remove } = useSystemAdminMutations();

  if (me && !me.isSystemAdmin) return <Alert severity="warning">システム管理者のみ利用できます。</Alert>;

  return (
    <Box sx={{ maxWidth: 800 }}>
      <PageHeader title="システム管理者" subtitle="カレンダーの新規追加・削除や、日種別・祝日マスタの管理ができる利用者です（複数人設定可能）。未登録の状態では最初にログインした利用者が登録されます。" />
      <Box sx={{ display: 'flex', mb: 2 }}>
        <UserPicker
          label="システム管理者を追加"
          excludeKeys={admins.map((a) => a.userKey)}
          onSelect={(u) => add.mutate({ userKey: u.userKey, displayName: u.displayName })}
        />
      </Box>
      {add.error && <Alert severity="error" sx={{ mb: 2 }}>{add.error.message}</Alert>}
      {remove.error && <Alert severity="error" sx={{ mb: 2 }}>{remove.error.message}</Alert>}
      <Paper>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>氏名</TableCell>
              <TableCell>ユーザー</TableCell>
              <TableCell sx={{ display: { xs: 'none', sm: 'table-cell' } }}>登録日時</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {admins.map((a) => (
              <TableRow key={a.id} hover>
                <TableCell>{a.displayName}</TableCell>
                <TableCell>{a.userKey}</TableCell>
                <TableCell sx={{ display: { xs: 'none', sm: 'table-cell' } }}>{fmtDateTime(a.createdAt)}</TableCell>
                <TableCell align="right">
                  <Tooltip title={a.userKey === me?.userKey ? '自分自身は削除できません' : '削除'}>
                    <span>
                      <IconButton size="small" disabled={a.userKey === me?.userKey} onClick={() => remove.mutate(a.id)}>
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    </span>
                  </Tooltip>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>

      <DataMaintenanceSection />
    </Box>
  );
}

/** 確認のために入力させる文字列（誤操作防止） */
const CLEAR_CONFIRM_TEXT = 'クリア';

/** データ管理：データクリア・サンプルデータ挿入 */
function DataMaintenanceSection() {
  const { clear, insertSample } = useDataMaintenance();
  const snackbar = useSnackbar();
  const [dialog, setDialog] = useState<'clear' | 'sample' | null>(null);
  const [confirmText, setConfirmText] = useState('');
  const busy = clear.isPending || insertSample.isPending;

  const close = () => {
    setDialog(null);
    setConfirmText('');
  };
  const run = () => {
    const mutation = dialog === 'clear' ? clear : insertSample;
    mutation.mutate(undefined, {
      onSuccess: (r) => {
        snackbar.show(r.message);
        close();
      },
      onError: (e) => {
        snackbar.show(e.message, 'error');
        close();
      },
    });
  };

  return (
    <Paper sx={{ p: 2, mt: 3 }}>
      <Typography variant="h3" sx={{ mb: 0.5 }}>
        データ管理
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        テストや運用開始前の準備に使います。システム管理者と利用者の登録は削除されません。
      </Typography>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
        <Button variant="outlined" startIcon={<PlaylistAddIcon />} disabled={busy} onClick={() => setDialog('sample')}>
          サンプルデータ挿入
        </Button>
        <Button variant="outlined" color="error" startIcon={<DeleteSweepIcon />} disabled={busy} onClick={() => setDialog('clear')}>
          データクリア
        </Button>
      </Stack>

      <Dialog open={dialog === 'sample'} onClose={busy ? undefined : close} fullWidth maxWidth="xs">
        <DialogTitle>サンプルデータ挿入</DialogTitle>
        <DialogContent>
          <DialogContentText>
            本社・滋賀・小牧などのサンプルカレンダーと日別設定を登録します。本社カレンダーの管理者にはあなたが設定されます。
            <br />
            カレンダーが 1 件でも登録されている場合は挿入できません（先にデータクリアしてください）。
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={close} disabled={busy}>
            キャンセル
          </Button>
          <Button variant="contained" onClick={run} disabled={busy}>
            挿入する
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={dialog === 'clear'} onClose={busy ? undefined : close} fullWidth maxWidth="xs">
        <DialogTitle>データクリア</DialogTitle>
        <DialogContent>
          <Alert severity="error" sx={{ mb: 2 }}>
            この操作は元に戻せません。
          </Alert>
          <DialogContentText sx={{ mb: 2 }}>
            カレンダー・日別設定・カレンダー管理者・日種別・祝日・通知・変更履歴をすべて削除し、日種別と祝日を初期値に戻します。
            システム管理者と利用者の登録は残ります。
          </DialogContentText>
          <TextField
            fullWidth
            size="small"
            label={`確認のため「${CLEAR_CONFIRM_TEXT}」と入力`}
            value={confirmText}
            onChange={(e) => setConfirmText(e.target.value)}
            autoComplete="off"
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={close} disabled={busy}>
            キャンセル
          </Button>
          <Button variant="contained" color="error" onClick={run} disabled={busy || confirmText !== CLEAR_CONFIRM_TEXT}>
            すべて削除する
          </Button>
        </DialogActions>
      </Dialog>
      {snackbar.element}
    </Paper>
  );
}
