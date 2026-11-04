import { Box, Tooltip, Typography } from '@mui/material';
import StickyNote2Icon from '@mui/icons-material/StickyNote2';
import type { Dayjs } from 'dayjs';
import dayjs from 'dayjs';
import type { ResolvedDay } from '../api/types';
import { WEEKDAYS, fmt, monthMatrix } from '../utils/date';
import { COLORS, dayVisual } from '../utils/dayStyle';

interface Props {
  year: number;
  month: number;
  days: Map<string, ResolvedDay>;
  onDayClick: (date: Dayjs) => void;
}

/** 月表示（日付クリックで編集） */
export function MonthCalendar({ year, month, days, onDayClick }: Props) {
  const cells = monthMatrix(year, month);
  const today = fmt(dayjs());

  return (
    <Box sx={{ border: '1px solid #E4E2EF', borderRadius: 1, overflow: 'hidden', bgcolor: 'background.paper' }}>
      <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(7, minmax(0, 1fr))', bgcolor: '#F1EFFA' }}>
        {WEEKDAYS.map((w, i) => (
          <Typography
            key={w}
            variant="body2"
            sx={{
              textAlign: 'center',
              py: 0.75,
              fontWeight: 700,
              color: i === 0 ? COLORS.sunday : i === 6 ? COLORS.saturday : 'text.primary',
            }}
          >
            {w}
          </Typography>
        ))}
      </Box>
      <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(7, minmax(0, 1fr))' }}>
        {cells.map((d) => {
          const key = fmt(d);
          const day = days.get(key);
          const inMonth = d.month() + 1 === month;
          const v = dayVisual(day, d.day());
          const notes = day?.notes ?? [];
          const tooltip = [
            day?.name ?? day?.dayTypeName,
            day?.sourceCalendarName && !day.hasOwnSetting ? `（${day.sourceCalendarName}の設定）` : null,
            ...notes.map((n) => `📝 ${n.calendarName}：${n.text}`),
          ]
            .filter(Boolean)
            .join('\n');
          return (
            <Tooltip key={key} title={tooltip ? <span style={{ whiteSpace: 'pre-line' }}>{tooltip}</span> : ''} disableInteractive>
              <Box
                role="button"
                tabIndex={0}
                aria-label={`${key} ${day?.name ?? ''}`}
                onClick={() => onDayClick(d)}
                onKeyDown={(e) => (e.key === 'Enter' || e.key === ' ') && onDayClick(d)}
                sx={{
                  minHeight: { xs: 56, sm: 84, md: 100 },
                  p: { xs: 0.5, sm: 0.75 },
                  borderTop: '1px solid #ECEAF4',
                  borderLeft: d.day() === 0 ? 'none' : '1px solid #ECEAF4',
                  bgcolor: v.background,
                  opacity: inMonth ? 1 : 0.4,
                  cursor: 'pointer',
                  position: 'relative',
                  outline: key === today ? '2px solid' : 'none',
                  outlineColor: 'primary.main',
                  outlineOffset: -2,
                  transition: 'box-shadow .15s',
                  '&:hover, &:focus-visible': { boxShadow: 'inset 0 0 0 2px rgba(81,72,157,0.5)' },
                }}
              >
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                  <Typography sx={{ fontWeight: 700, color: v.dateColor, fontSize: { xs: 13, sm: 15 } }}>{d.date()}</Typography>
                  {day?.hasOwnSetting && (
                    <Box title="このカレンダーで個別設定" sx={{ width: 6, height: 6, borderRadius: '50%', bgcolor: 'primary.main' }} />
                  )}
                  {notes.length > 0 && <StickyNote2Icon sx={{ fontSize: 14, color: 'secondary.main', ml: 'auto' }} />}
                </Box>
                {v.label && (
                  <Typography
                    sx={{
                      fontSize: { xs: 10, sm: 12 },
                      lineHeight: 1.3,
                      color: v.accent ?? 'text.primary',
                      fontWeight: 600,
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      display: '-webkit-box',
                      WebkitLineClamp: 2,
                      WebkitBoxOrient: 'vertical',
                    }}
                  >
                    {v.label}
                  </Typography>
                )}
                {notes.length > 0 && (
                  <Typography
                    sx={{
                      display: { xs: 'none', sm: '-webkit-box' },
                      fontSize: 11,
                      color: 'text.secondary',
                      overflow: 'hidden',
                      WebkitLineClamp: 2,
                      WebkitBoxOrient: 'vertical',
                    }}
                  >
                    {notes[notes.length - 1].text}
                  </Typography>
                )}
              </Box>
            </Tooltip>
          );
        })}
      </Box>
    </Box>
  );
}
