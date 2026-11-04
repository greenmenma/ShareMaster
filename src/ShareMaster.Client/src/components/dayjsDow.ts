import dayjs from 'dayjs';

/** "YYYY-MM-DD" の曜日（0=日） */
export const dayjsDow = (date: string) => dayjs(date).day();
