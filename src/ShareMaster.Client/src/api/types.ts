// サーバー API の型定義（ShareMaster.Server/Contracts/Dtos.cs と対応）

export type AuthMode = 'Development' | 'EntraId';

export interface DevUser {
  userKey: string;
  displayName: string;
}

export interface AuthConfig {
  mode: AuthMode;
  clientId?: string | null;
  authority?: string | null;
  scopes: string[];
  devUsers: DevUser[];
}

export interface Me {
  userKey: string;
  displayName: string;
  email?: string | null;
  isSystemAdmin: boolean;
}

export interface User {
  userKey: string;
  displayName: string;
  email?: string | null;
}

export interface Manager {
  userKey: string;
  displayName: string;
  receiveNotifications: boolean;
}

export interface Calendar {
  id: number;
  code: string;
  name: string;
  description?: string | null;
  parentId?: number | null;
  sortOrder: number;
  weeklyHolidays?: string | null;
  usePublicHolidays?: boolean | null;
  effectiveWeeklyHolidays: number[];
  effectiveUsePublicHolidays: boolean;
  managers: Manager[];
  canEdit: boolean;
  updatedAt: string;
  updatedBy: string;
}

export interface CalendarSaveRequest {
  code: string;
  name: string;
  description?: string | null;
  parentId?: number | null;
  sortOrder: number;
  weeklyHolidays?: number[] | null;
  usePublicHolidays?: boolean | null;
  managers: Manager[];
}

export type DaySource = 'Default' | 'Weekly' | 'PublicHoliday' | 'Override';

export interface ResolvedNote {
  calendarId: number;
  calendarName: string;
  text: string;
}

export interface ResolvedDay {
  date: string;
  isHoliday: boolean;
  source: DaySource;
  dayTypeId?: number | null;
  dayTypeName?: string | null;
  color?: string | null;
  name?: string | null;
  sourceCalendarId?: number | null;
  sourceCalendarName?: string | null;
  hasOwnSetting: boolean;
  notes: ResolvedNote[];
}

export interface CalendarDays {
  calendarId: number;
  days: ResolvedDay[];
}

export interface OwnSetting {
  dayTypeId?: number | null;
  name?: string | null;
  note?: string | null;
  updatedAt: string;
  updatedBy: string;
}

export interface DayDetail {
  calendarId: number;
  date: string;
  effective: ResolvedDay;
  inherited?: ResolvedDay | null;
  own?: OwnSetting | null;
  canEdit: boolean;
}

export type DayUpdateMode = 'Inherit' | 'Set';

export interface DayUpdateRequest {
  from: string;
  to?: string | null;
  daysOfWeek?: number[] | null;
  mode: DayUpdateMode;
  dayTypeId?: number | null;
  name?: string | null;
  note?: string | null;
}

export interface DayUpdateResult {
  updatedCount: number;
  notifiedUserCount: number;
}

export interface YearRange {
  minYear: number;
  maxYear: number;
}

export interface DayType {
  id: number;
  code: string;
  name: string;
  color: string;
  isHoliday: boolean;
  sortOrder: number;
  isSystem: boolean;
}

export type DayTypeSaveRequest = Omit<DayType, 'id' | 'isSystem'>;

export interface PublicHoliday {
  id: number;
  date: string;
  name: string;
}

export interface History {
  id: number;
  calendarId?: number | null;
  calendarName?: string | null;
  entityType: string;
  entityKey: string;
  action: 'Create' | 'Update' | 'Delete';
  summary: string;
  beforeJson?: string | null;
  afterJson?: string | null;
  changedBy: string;
  changedByName: string;
  changedAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface Notification {
  id: number;
  calendarId?: number | null;
  title: string;
  message: string;
  linkUrl?: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface NotificationSummary {
  unreadCount: number;
  items: Notification[];
}

export interface SystemAdmin {
  id: number;
  userKey: string;
  displayName: string;
  createdAt: string;
  createdBy: string;
}
