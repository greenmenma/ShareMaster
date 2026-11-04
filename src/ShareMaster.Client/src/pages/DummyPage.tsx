import { Box, Paper, Typography } from '@mui/material';
import ConstructionIcon from '@mui/icons-material/Construction';
import { PageHeader } from '../components/PageHeader';

/** 未実装メニューのダミー画面 */
export function DummyPage({ title }: { title: string }) {
  return (
    <Box>
      <PageHeader title={title} />
      <Paper sx={{ p: 6, textAlign: 'center' }}>
        <ConstructionIcon sx={{ fontSize: 56, color: 'primary.light' }} />
        <Typography variant="h2" sx={{ mt: 1 }}>準備中</Typography>
        <Typography color="text.secondary" sx={{ mt: 1 }}>
          「{title}」マスタは今後追加予定です。
        </Typography>
      </Paper>
    </Box>
  );
}
