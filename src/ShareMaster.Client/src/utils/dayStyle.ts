// 日付セルの配色ルール
//  - 日曜日・祝日：赤 / 土曜日：青
//  - 個別設定の休日：日種別に設定した色
//  - 日曜・土曜以外の定休日：グレー
import type { ResolvedDay } from '../api/types';

export const COLORS = {
  sunday: '#D32F2F',
  sundayBg: '#FDECEC',
  saturday: '#1565C0',
  saturdayBg: '#E7F0FC',
  weeklyOtherBg: '#EEEEEE',
  publicHoliday: '#D32F2F',
  publicHolidayBg: '#FDECEC',
} as const;

/** #RRGGBB に透明度を付与した rgba 文字列 */
export function alpha(hex: string, a: number): string {
  const m = /^#?([0-9a-f]{2})([0-9a-f]{2})([0-9a-f]{2})$/i.exec(hex);
  if (!m) return hex;
  const [r, g, b] = [m[1], m[2], m[3]].map((x) => parseInt(x, 16));
  return `rgba(${r}, ${g}, ${b}, ${a})`;
}

export interface DayVisual {
  /** 日付の文字色 */
  dateColor: string | undefined;
  /** セル背景色 */
  background: string | undefined;
  /** 種別のアクセント色（凡例・チップ用） */
  accent: string | undefined;
  /** セルに表示する短いラベル */
  label: string | undefined;
}

export function dayVisual(day: ResolvedDay | undefined, dow: number): DayVisual {
  const isPublic = day?.source === 'PublicHoliday';
  const dateColor = dow === 0 || isPublic ? COLORS.sunday : dow === 6 ? COLORS.saturday : undefined;
  if (!day) return { dateColor, background: undefined, accent: undefined, label: undefined };

  if (day.source === 'Override' && day.color) {
    return {
      dateColor,
      background: day.isHoliday ? alpha(day.color, 0.16) : undefined,
      accent: day.color,
      label: day.name ?? day.dayTypeName ?? undefined,
    };
  }
  if (isPublic) return { dateColor, background: COLORS.publicHolidayBg, accent: COLORS.publicHoliday, label: day.name ?? '祝日' };
  if (day.isHoliday) {
    if (dow === 0) return { dateColor, background: COLORS.sundayBg, accent: COLORS.sunday, label: day.name ?? undefined };
    if (dow === 6) return { dateColor, background: COLORS.saturdayBg, accent: COLORS.saturday, label: day.name ?? undefined };
    return { dateColor, background: COLORS.weeklyOtherBg, accent: '#757575', label: day.name ?? '定休日' };
  }
  return { dateColor, background: undefined, accent: undefined, label: day.name ?? undefined };
}

/** 一覧などで「休日または特記事項あり」の日かどうか */
export const isNotable = (d: ResolvedDay | undefined) =>
  !!d && (d.isHoliday || d.source === 'Override' || d.notes.length > 0 || !!d.name);
