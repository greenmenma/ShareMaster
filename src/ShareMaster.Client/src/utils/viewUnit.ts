// 表示単位（すべて／年／月）と表示期間を URL のクエリで管理する
import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router';
import dayjs from 'dayjs';
import type { YearRange } from '../api/types';

export type ViewUnit = 'all' | 'year' | 'month';

export interface ViewState {
  unit: ViewUnit;
  year: number;
  month: number;
}

export function useViewState(defaultUnit: ViewUnit) {
  const [sp, setSp] = useSearchParams();
  const today = dayjs();
  const unitParam = sp.get('unit');
  const unit: ViewUnit = unitParam === 'all' || unitParam === 'year' || unitParam === 'month' ? unitParam : defaultUnit;
  const year = Number(sp.get('y')) || today.year();
  const month = Math.min(12, Math.max(1, Number(sp.get('m')) || today.month() + 1));

  const set = useCallback(
    (next: Partial<ViewState>) => {
      const s = { unit, year, month, ...next };
      setSp(
        (prev) => {
          const p = new URLSearchParams(prev);
          p.set('unit', s.unit);
          p.set('y', String(s.year));
          p.set('m', String(s.month));
          return p;
        },
        { replace: true },
      );
    },
    [unit, year, month, setSp],
  );

  return useMemo(() => ({ unit, year, month, set }), [unit, year, month, set]);
}

/** 表示単位に応じた期間 */
export function viewRange(v: ViewState, range: YearRange | undefined): { from: dayjs.Dayjs; to: dayjs.Dayjs } {
  if (v.unit === 'month') {
    const from = dayjs(new Date(v.year, v.month - 1, 1));
    return { from, to: from.endOf('month') };
  }
  if (v.unit === 'year') {
    return { from: dayjs(new Date(v.year, 0, 1)), to: dayjs(new Date(v.year, 11, 31)) };
  }
  const min = range?.minYear ?? v.year;
  const max = range?.maxYear ?? v.year;
  return { from: dayjs(new Date(min, 0, 1)), to: dayjs(new Date(max, 11, 31)) };
}
