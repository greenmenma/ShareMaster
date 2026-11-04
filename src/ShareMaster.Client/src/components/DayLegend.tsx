import { Box, Typography } from '@mui/material';
import { useDayTypes } from '../api/queries';
import { COLORS, alpha } from '../utils/dayStyle';

function Swatch({ bg, border, label }: { bg: string; border?: string; label: string }) {
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
      <Box sx={{ width: 14, height: 14, borderRadius: 0.5, bgcolor: bg, border: `2px solid ${border ?? bg}` }} />
      <Typography variant="caption">{label}</Typography>
    </Box>
  );
}

/** 凡例（日曜・土曜・祝日・日種別ごとの色） */
export function DayLegend() {
  const { data: types = [] } = useDayTypes();
  return (
    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5, alignItems: 'center' }}>
      <Swatch bg={COLORS.sundayBg} border={COLORS.sunday} label="日曜・祝日" />
      <Swatch bg={COLORS.saturdayBg} border={COLORS.saturday} label="土曜" />
      <Swatch bg={COLORS.weeklyOtherBg} border="#757575" label="その他定休日" />
      {types.map((t) => (
        <Swatch key={t.id} bg={t.isHoliday ? alpha(t.color, 0.16) : '#fff'} border={t.color} label={t.name} />
      ))}
    </Box>
  );
}
