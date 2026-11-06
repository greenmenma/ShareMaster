import { describe, expect, it } from 'vitest';
import { fiscalPeriodLabelOf, fiscalPeriodLabelOfYear, fiscalPeriodOf } from './fiscalPeriod';

describe('fiscalPeriodOf', () => {
  it('2024 年までは 3 月決算', () => {
    expect(fiscalPeriodOf(2022, 4)).toBe(107);
    expect(fiscalPeriodOf(2023, 3)).toBe(107);
    expect(fiscalPeriodOf(2023, 4)).toBe(108);
    expect(fiscalPeriodOf(2024, 3)).toBe(108);
  });
  it('2024 年 4〜12 月は 109 期', () => {
    expect(fiscalPeriodOf(2024, 4)).toBe(109);
    expect(fiscalPeriodOf(2024, 12)).toBe(109);
  });
  it('2025 年以降は 12 月決算', () => {
    expect(fiscalPeriodOf(2025, 1)).toBe(110);
    expect(fiscalPeriodOf(2025, 12)).toBe(110);
    expect(fiscalPeriodOf(2026, 1)).toBe(111);
    expect(fiscalPeriodOf(2026, 12)).toBe(111);
  });
});

describe('fiscalPeriodLabelOfYear', () => {
  it('暦年にまたがる期を並べて表示', () => {
    expect(fiscalPeriodLabelOfYear(2023)).toBe('107・108期');
    expect(fiscalPeriodLabelOfYear(2024)).toBe('108・109期');
    expect(fiscalPeriodLabelOfYear(2025)).toBe('110期');
    expect(fiscalPeriodLabelOfYear(2026)).toBe('111期');
  });
  it('月の期', () => {
    expect(fiscalPeriodLabelOf(2024, 3)).toBe('108期');
  });
});
