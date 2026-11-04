import { Link as RouterLink } from 'react-router';
import { Box, Button, Typography } from '@mui/material';

export function NotFoundPage() {
  return (
    <Box sx={{ textAlign: 'center', py: 8 }}>
      <Typography variant="h1">ページが見つかりません</Typography>
      <Button component={RouterLink} to="/" sx={{ mt: 2 }} variant="contained">ホームへ</Button>
    </Box>
  );
}
