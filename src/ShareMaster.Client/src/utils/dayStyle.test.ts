import { describe, expect, it } from 'vitest';
import { COLORS, alpha, dayVisual } from './dayStyle';
import type { ResolvedDay } from '../api/types';

const base: ResolvedDay = { date: '2026-11-01', isHoliday: false, source: 'Default', hasOwnSetting: false, notes: [] };

describe('dayVisual', () => {
  it('日曜は赤・土曜は青', () => {
    expect(dayVisual({ ...base, isHoliday: true, source: 'Weekly' }, 0).dateColor).toBe(COLORS.sunday);
    expect(dayVisual({ ...base, isHoliday: true, source: 'Weekly' }, 6).background).toBe(COLORS.saturdayBg);
  });
  it('祝日は平日でも赤', () => {
    const v = dayVisual({ ...base, isHoliday: true, source: 'PublicHoliday', name: '文化の日' }, 2);
    expect(v.dateColor).toBe(COLORS.sunday);
    expect(v.label).toBe('文化の日');
  });
  it('個別設定の休日は日種別の色', () => {
    const v = dayVisual({ ...base, isHoliday: true, source: 'Override', color: '#2E7D32', dayTypeName: '会社休日' }, 3);
    expect(v.background).toBe(alpha('#2E7D32', 0.16));
    expect(v.label).toBe('会社休日');
  });
});
