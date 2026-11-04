import { Chip } from '@mui/material';
import type { ResolvedDay } from '../api/types';
import { dayjsDow } from './dayjsDow';
import { dayVisual } from '../utils/dayStyle';

/** 確定済みの日区分を表すチップ */
export function DayChip({ day }: { day: ResolvedDay }) {
  const v = dayVisual(day, dayjsDow(day.date));
  const label = day.isHoliday
    ? (day.name ?? day.dayTypeName ?? '休日')
    : day.source === 'Override'
      ? (day.dayTypeName ?? '稼働日') + (day.name && day.name !== day.dayTypeName ? `：${day.name}` : '')
      : '稼働日';
  return (
    <Chip
      size="small"
      label={label}
      sx={{
        bgcolor: v.background ?? '#fff',
        color: v.accent ?? 'text.primary',
        border: `1px solid ${v.accent ?? '#ccc'}`,
        fontWeight: 700,
      }}
    />
  );
}
