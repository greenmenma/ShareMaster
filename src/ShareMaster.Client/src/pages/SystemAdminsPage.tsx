import { Alert, Box, IconButton, Paper, Table, TableBody, TableCell, TableHead, TableRow, Tooltip } from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import { useMe, useSystemAdminMutations, useSystemAdmins } from '../api/queries';
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
      <PageHeader title="システム管理者" subtitle="カレンダーの新規追加・削除や、日種別・祝日マスタの管理ができる利用者です（複数人設定可能）。" />
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
    </Box>
  );
}
