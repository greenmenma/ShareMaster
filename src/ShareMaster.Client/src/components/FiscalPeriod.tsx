import { Box } from '@mui/material';
import { fiscalPeriodLabelOf, fiscalPeriodLabelOfYear } from '../utils/fiscalPeriod';

/** 年（月）の横に添える期の表示。month を省略すると暦年に含まれる期を表示 */
export function FiscalPeriod({ year, month }: { year: number; month?: number }) {
  return (
    <Box component="span" sx={{ ml: 0.75, fontSize: '0.75em', fontWeight: 400, color: 'text.secondary', whiteSpace: 'nowrap' }}>
      （{month ? fiscalPeriodLabelOf(year, month) : fiscalPeriodLabelOfYear(year)}）
    </Box>
  );
}
