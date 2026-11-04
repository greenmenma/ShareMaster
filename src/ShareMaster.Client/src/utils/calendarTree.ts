import type { Calendar } from '../api/types';

export interface CalendarTreeNode {
  calendar: Calendar;
  depth: number;
  children: CalendarTreeNode[];
}

/** フラットなカレンダー一覧から木構造を作る（並び順は sortOrder → id） */
export function buildTree(calendars: Calendar[]): CalendarTreeNode[] {
  const ids = new Set(calendars.map((c) => c.id));
  const byParent = new Map<number | null, Calendar[]>();
  for (const c of calendars) {
    const p = c.parentId && ids.has(c.parentId) ? c.parentId : null;
    byParent.set(p, [...(byParent.get(p) ?? []), c]);
  }
  const make = (parent: number | null, depth: number, seen: Set<number>): CalendarTreeNode[] =>
    (byParent.get(parent) ?? [])
      .filter((c) => !seen.has(c.id))
      .sort((a, b) => a.sortOrder - b.sortOrder || a.id - b.id)
      .map((c) => ({ calendar: c, depth, children: make(c.id, depth + 1, new Set([...seen, c.id])) }));
  return make(null, 0, new Set());
}

/** 木構造を表示順（深さ優先）に平坦化する */
export function flattenTree(nodes: CalendarTreeNode[]): CalendarTreeNode[] {
  return nodes.flatMap((n) => [n, ...flattenTree(n.children)]);
}

/** ルートから対象までのカレンダー経路 */
export function lineage(calendars: Calendar[], id: number): Calendar[] {
  const map = new Map(calendars.map((c) => [c.id, c]));
  const path: Calendar[] = [];
  const seen = new Set<number>();
  let cur = map.get(id);
  while (cur && !seen.has(cur.id)) {
    seen.add(cur.id);
    path.unshift(cur);
    cur = cur.parentId ? map.get(cur.parentId) : undefined;
  }
  return path;
}

/** 対象と子孫の ID */
export function descendantIds(calendars: Calendar[], id: number): Set<number> {
  const result = new Set<number>([id]);
  let added = true;
  while (added) {
    added = false;
    for (const c of calendars) {
      if (c.parentId && result.has(c.parentId) && !result.has(c.id)) {
        result.add(c.id);
        added = true;
      }
    }
  }
  return result;
}
