// 会社の期（決算期）の計算
// - 2024 年 3 月までは 3 月決算（4/1〜翌 3/31）
// - 2024 年は決算期変更のため 4/1〜12/31 の 9 か月（109 期）
// - 2025 年以降は 12 月決算（1/1〜12/31）

/** 年月が属する期 */
export function fiscalPeriodOf(year: number, month: number): number {
  if (year >= 2025) return year - 1915;
  return month >= 4 ? year - 1915 : year - 1916;
}

/** 暦年に含まれる期（昇順・重複なし） */
export function fiscalPeriodsOfYear(year: number): number[] {
  const first = fiscalPeriodOf(year, 1);
  const last = fiscalPeriodOf(year, 12);
  return first === last ? [first] : [first, last];
}

/** 暦年の期の表示（例："107・108期"、"110期"） */
export const fiscalPeriodLabelOfYear = (year: number) => `${fiscalPeriodsOfYear(year).join('・')}期`;

/** 年月の期の表示（例："108期"） */
export const fiscalPeriodLabelOf = (year: number, month: number) => `${fiscalPeriodOf(year, month)}期`;
