import { describe, expect, it } from 'vitest';
import { buildTree, descendantIds, flattenTree, lineage } from './calendarTree';
import type { Calendar } from '../api/types';

const cal = (id: number, name: string, parentId: number | null, sortOrder = id): Calendar => ({
  id, code: `C${id}`, name, parentId, sortOrder, managers: [], canEdit: false,
  effectiveWeeklyHolidays: [0, 6], effectiveUsePublicHolidays: true, updatedAt: '', updatedBy: '',
});

const data = [cal(1, '本社', null), cal(2, '滋賀', 1), cal(3, 'システムA', 2), cal(4, '小牧', 1), cal(5, 'システムC', 1, 0)];

describe('calendarTree', () => {
  it('木構造を表示順に平坦化できる', () => {
    const flat = flattenTree(buildTree(data)).map((n) => `${n.depth}:${n.calendar.name}`);
    expect(flat).toEqual(['0:本社', '1:システムC', '1:滋賀', '2:システムA', '1:小牧']);
  });
  it('祖先の経路を取得できる', () => {
    expect(lineage(data, 3).map((c) => c.name)).toEqual(['本社', '滋賀', 'システムA']);
  });
  it('子孫を取得できる', () => {
    expect([...descendantIds(data, 2)].sort()).toEqual([2, 3]);
  });
});
