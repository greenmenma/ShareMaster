import type { ReactNode } from 'react';
import { Box, Button, IconButton, ToggleButton, ToggleButtonGroup, Tooltip, Typography } from '@mui/material';
import ChevronLeftIcon from '@mui/icons-material/ChevronLeft';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import dayjs from 'dayjs';
import type { ViewState, ViewUnit } from '../utils/viewUnit';
import type { YearRange } from '../api/types';

interface Props {
  view: ViewState & { set: (v: Partial<ViewState>) => void };
  range?: YearRange;
  children?: ReactNode;
}

/** 表示単位（すべて／年／月）の切替と期間移動 */
export function ViewUnitBar({ view, range, children }: Props) {
  const { unit, year, month, set } = view;

  const move = (delta: number) => {
    if (unit === 'year') set({ year: year + delta });
    else if (unit === 'month') {
      const d = dayjs(new Date(year, month - 1, 1)).add(delta, 'month');
      set({ year: d.year(), month: d.month() + 1 });
    }
  };

  const label =
    unit === 'month' ? `${year}年${month}月` : unit === 'year' ? `${year}年` : range ? `${range.minYear}年〜${range.maxYear}年` : 'すべて';

  return (
    <Box sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 1, mb: 2 }}>
      <ToggleButtonGroup
        size="small"
        exclusive
        color="primary"
        value={unit}
        onChange={(_, v: ViewUnit | null) => v && set({ unit: v })}
        aria-label="表示単位"
      >
        <ToggleButton value="all" sx={{ px: 2 }}>すべて</ToggleButton>
        <ToggleButton value="year" sx={{ px: 2 }}>年</ToggleButton>
        <ToggleButton value="month" sx={{ px: 2 }}>月</ToggleButton>
      </ToggleButtonGroup>

      <Box sx={{ display: 'flex', alignItems: 'center' }}>
        <Tooltip title="前へ">
          <span>
            <IconButton size="small" onClick={() => move(-1)} disabled={unit === 'all'} aria-label="前へ">
              <ChevronLeftIcon />
            </IconButton>
          </span>
        </Tooltip>
        <Typography sx={{ minWidth: 120, textAlign: 'center', fontWeight: 700, fontSize: 18 }}>{label}</Typography>
        <Tooltip title="次へ">
          <span>
            <IconButton size="small" onClick={() => move(1)} disabled={unit === 'all'} aria-label="次へ">
              <ChevronRightIcon />
            </IconButton>
          </span>
        </Tooltip>
      </Box>
      <Button
        size="small"
        variant="outlined"
        onClick={() => set({ year: dayjs().year(), month: dayjs().month() + 1, unit: unit === 'all' ? 'month' : unit })}
      >
        今日
      </Button>
      <Box sx={{ flex: 1 }} />
      {children}
    </Box>
  );
}
