// TanStack Query を用いた API 呼び出しフック
import { useMutation, useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { api, qs } from './http';
import type {
  Calendar,
  CalendarDays,
  CalendarSaveRequest,
  DayDetail,
  DayType,
  DayTypeSaveRequest,
  DayUpdateRequest,
  DayUpdateResult,
  History,
  Me,
  NotificationSummary,
  PagedResult,
  PublicHoliday,
  SystemAdmin,
  User,
  YearRange,
} from './types';

export const keys = {
  me: ['me'] as const,
  calendars: ['calendars'] as const,
  calendar: (id: number) => ['calendars', id] as const,
  days: ['days'] as const,
  dayTypes: ['day-types'] as const,
  publicHolidays: ['public-holidays'] as const,
  histories: ['histories'] as const,
  notifications: ['notifications'] as const,
  systemAdmins: ['system-admins'] as const,
};

export const useMe = () => useQuery({ queryKey: keys.me, queryFn: () => api<Me>('/api/me'), staleTime: 5 * 60_000 });

export const useCalendars = () =>
  useQuery({ queryKey: keys.calendars, queryFn: () => api<Calendar[]>('/api/calendars'), staleTime: 60_000 });

export const useCalendar = (id: number | undefined) =>
  useQuery({
    queryKey: keys.calendar(id ?? 0),
    queryFn: () => api<Calendar>(`/api/calendars/${id}`),
    enabled: !!id,
  });

/** 期間内の確定済み日情報（ids 省略時は全カレンダー） */
export const useDays = (from: string, to: string, ids?: number[]) =>
  useQuery({
    queryKey: [...keys.days, from, to, ids ?? 'all'],
    queryFn: () => api<CalendarDays[]>(`/api/calendars/days${qs({ from, to, ids })}`),
    placeholderData: keepPreviousData,
  });

export const useDayDetail = (calendarId: number, date: string | null) =>
  useQuery({
    queryKey: [...keys.days, 'detail', calendarId, date],
    queryFn: () => api<DayDetail>(`/api/calendars/${calendarId}/days/${date}`),
    enabled: !!date && calendarId > 0,
  });

export const useYearRange = () =>
  useQuery({ queryKey: [...keys.days, 'year-range'], queryFn: () => api<YearRange>('/api/calendars/year-range') });

export const useDayTypes = () =>
  useQuery({ queryKey: keys.dayTypes, queryFn: () => api<DayType[]>('/api/day-types'), staleTime: 5 * 60_000 });

export const usePublicHolidays = (year: number) =>
  useQuery({
    queryKey: [...keys.publicHolidays, year],
    queryFn: () => api<PublicHoliday[]>(`/api/public-holidays${qs({ year })}`),
  });

export const useHistories = (params: { calendarId?: number; entityType?: string; keyword?: string; page: number; pageSize?: number }) =>
  useQuery({
    queryKey: [...keys.histories, params],
    queryFn: () => api<PagedResult<History>>(`/api/histories${qs({ ...params })}`),
    placeholderData: keepPreviousData,
  });

export const useNotifications = () =>
  useQuery({
    queryKey: keys.notifications,
    queryFn: () => api<NotificationSummary>('/api/notifications'),
    refetchInterval: 60_000,
  });

export const useSystemAdmins = (enabled: boolean) =>
  useQuery({ queryKey: keys.systemAdmins, queryFn: () => api<SystemAdmin[]>('/api/system-admins'), enabled });

export const searchUsers = (search: string) => api<User[]>(`/api/users${qs({ search })}`);

// ---- 更新系 ----

export function useSaveCalendar() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id?: number; body: CalendarSaveRequest }) =>
      id
        ? api<Calendar>(`/api/calendars/${id}`, { method: 'PUT', json: body })
        : api<Calendar>('/api/calendars', { method: 'POST', json: body }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: keys.calendars });
      qc.invalidateQueries({ queryKey: keys.days });
      qc.invalidateQueries({ queryKey: keys.histories });
    },
  });
}

export function useDeleteCalendar() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => api<void>(`/api/calendars/${id}`, { method: 'DELETE' }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: keys.calendars });
      qc.invalidateQueries({ queryKey: keys.days });
      qc.invalidateQueries({ queryKey: keys.histories });
    },
  });
}

export function useUpdateDays(calendarId: number) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: DayUpdateRequest) =>
      api<DayUpdateResult>(`/api/calendars/${calendarId}/days`, { method: 'PUT', json: body }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: keys.days });
      qc.invalidateQueries({ queryKey: keys.histories });
      qc.invalidateQueries({ queryKey: keys.notifications });
    },
  });
}

export function useSaveDayType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id?: number; body: DayTypeSaveRequest }) =>
      id
        ? api<DayType>(`/api/day-types/${id}`, { method: 'PUT', json: body })
        : api<DayType>('/api/day-types', { method: 'POST', json: body }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: keys.dayTypes });
      qc.invalidateQueries({ queryKey: keys.days });
    },
  });
}

export function useDeleteDayType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => api<void>(`/api/day-types/${id}`, { method: 'DELETE' }),
    onSuccess: () => qc.invalidateQueries({ queryKey: keys.dayTypes }),
  });
}

export function usePublicHolidayMutations() {
  const qc = useQueryClient();
  const onSuccess = () => {
    qc.invalidateQueries({ queryKey: keys.publicHolidays });
    qc.invalidateQueries({ queryKey: keys.days });
    qc.invalidateQueries({ queryKey: keys.histories });
  };
  return {
    generate: useMutation({
      mutationFn: (year: number) =>
        api<{ added: number; skipped: number }>(`/api/public-holidays/generate${qs({ year })}`, { method: 'POST' }),
      onSuccess,
    }),
    save: useMutation({
      mutationFn: ({ id, body }: { id?: number; body: { date: string; name: string } }) =>
        id
          ? api<PublicHoliday>(`/api/public-holidays/${id}`, { method: 'PUT', json: body })
          : api<PublicHoliday>('/api/public-holidays', { method: 'POST', json: body }),
      onSuccess,
    }),
    remove: useMutation({
      mutationFn: (id: number) => api<void>(`/api/public-holidays/${id}`, { method: 'DELETE' }),
      onSuccess,
    }),
  };
}

export function useNotificationMutations() {
  const qc = useQueryClient();
  const onSuccess = () => qc.invalidateQueries({ queryKey: keys.notifications });
  return {
    markRead: useMutation({ mutationFn: (id: number) => api<void>(`/api/notifications/${id}/read`, { method: 'POST' }), onSuccess }),
    markAllRead: useMutation({ mutationFn: () => api<void>('/api/notifications/read-all', { method: 'POST' }), onSuccess }),
  };
}

export function useSystemAdminMutations() {
  const qc = useQueryClient();
  const onSuccess = () => {
    qc.invalidateQueries({ queryKey: keys.systemAdmins });
    qc.invalidateQueries({ queryKey: keys.me });
  };
  return {
    add: useMutation({
      mutationFn: (body: { userKey: string; displayName?: string }) =>
        api<SystemAdmin>('/api/system-admins', { method: 'POST', json: body }),
      onSuccess,
    }),
    remove: useMutation({ mutationFn: (id: number) => api<void>(`/api/system-admins/${id}`, { method: 'DELETE' }), onSuccess }),
  };
}
