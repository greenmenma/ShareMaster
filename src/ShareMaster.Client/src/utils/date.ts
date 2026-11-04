import dayjs, { type Dayjs } from 'dayjs';

export const WEEKDAYS = ['日', '月', '火', '水', '木', '金', '土'] as const;

export const fmt = (d: Dayjs) => d.format('YYYY-MM-DD');

/** "2026/11/04（水）" 形式 */
export const fmtJp = (d: Dayjs | string) => {
  const x = dayjs(d);
  return `${x.format('YYYY/MM/DD')}（${WEEKDAYS[x.day()]}）`;
};

export const fmtDateTime = (s: string) => dayjs(s).format('YYYY/MM/DD HH:mm');

/** 月表示用：月初を含む週の日曜から、月末を含む週の土曜までの日付 */
export function monthMatrix(year: number, month: number): Dayjs[] {
  const first = dayjs(new Date(year, month - 1, 1));
  const start = first.subtract(first.day(), 'day');
  const last = first.endOf('month');
  const end = last.add(6 - last.day(), 'day');
  const days: Dayjs[] = [];
  for (let d = start; !d.isAfter(end, 'day'); d = d.add(1, 'day')) days.push(d);
  return days;
}

export function daysBetween(from: Dayjs, to: Dayjs): Dayjs[] {
  const days: Dayjs[] = [];
  for (let d = from; !d.isAfter(to, 'day'); d = d.add(1, 'day')) days.push(d);
  return days;
}
