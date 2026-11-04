import { Box, Paper, Tooltip, Typography } from '@mui/material';
import type { Dayjs } from 'dayjs';
import dayjs from 'dayjs';
import type { ResolvedDay } from '../api/types';
import { WEEKDAYS, fmt, monthMatrix } from '../utils/date';
import { COLORS, dayVisual } from '../utils/dayStyle';

interface Props {
  year: number;
  days: Map<string, ResolvedDay>;
  onDayClick: (date: Dayjs) => void;
  onMonthClick?: (month: number) => void;
}

function MiniMonth({ year, month, days, onDayClick, onMonthClick }: Props & { month: number }) {
  const cells = monthMatrix(year, month);
  const today = fmt(dayjs());
  const holidays = cells.filter((d) => d.month() + 1 === month && days.get(fmt(d))?.isHoliday).length;

  return (
    <Paper sx={{ p: 1 }}>
      <Box sx={{ display: 'flex', alignItems: 'baseline', mb: 0.5 }}>
        <Typography
          onClick={() => onMonthClick?.(month)}
          sx={{ fontWeight: 700, flex: 1, cursor: onMonthClick ? 'pointer' : undefined, '&:hover': { color: 'primary.main' } }}
        >
          {month}月
        </Typography>
        <Typography variant="caption" color="text.secondary">
          休日 {holidays}日
        </Typography>
      </Box>
      <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(7, 1fr)', gap: '2px' }}>
        {WEEKDAYS.map((w, i) => (
          <Typography
            key={w}
            sx={{ fontSize: 11, textAlign: 'center', color: i === 0 ? COLORS.sunday : i === 6 ? COLORS.saturday : 'text.secondary' }}
          >
            {w}
          </Typography>
        ))}
        {cells.map((d) => {
          const key = fmt(d);
          if (d.month() + 1 !== month) return <Box key={key} />;
          const day = days.get(key);
          const v = dayVisual(day, d.day());
          const tip = [v.label, ...(day?.notes ?? []).map((n) => `📝 ${n.text}`)].filter(Boolean).join('\n');
          return (
            <Tooltip key={key} title={tip ? <span style={{ whiteSpace: 'pre-line' }}>{`${key}\n${tip}`}</span> : ''} disableInteractive>
              <Box
                role="button"
                tabIndex={0}
                aria-label={key}
                onClick={() => onDayClick(d)}
                onKeyDown={(e) => e.key === 'Enter' && onDayClick(d)}
                sx={{
                  aspectRatio: '1',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: 12,
                  borderRadius: 0.5,
                  cursor: 'pointer',
                  position: 'relative',
                  bgcolor: v.background,
                  color: v.dateColor,
                  fontWeight: day?.isHoliday ? 700 : 400,
                  outline: key === today ? '2px solid' : 'none',
                  outlineColor: 'primary.main',
                  boxShadow: day?.source === 'Override' && !day.isHoliday && v.accent ? `inset 0 -3px 0 ${v.accent}` : undefined,
                  '&:hover': { boxShadow: 'inset 0 0 0 2px rgba(81,72,157,0.5)' },
                }}
              >
                {d.date()}
                {(day?.notes.length ?? 0) > 0 && (
                  <Box sx={{ position: 'absolute', top: 1, right: 1, width: 5, height: 5, borderRadius: '50%', bgcolor: 'secondary.main' }} />
                )}
              </Box>
            </Tooltip>
          );
        })}
      </Box>
    </Paper>
  );
}

/** 年表示（12 か月のミニカレンダー） */
export function YearCalendar(props: Props) {
  return (
    <Box
      sx={{
        display: 'grid',
        gap: 1.5,
        gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(3, 1fr)', xl: 'repeat(4, 1fr)' },
      }}
    >
      {Array.from({ length: 12 }, (_, i) => (
        <MiniMonth key={i} {...props} month={i + 1} />
      ))}
    </Box>
  );
}
