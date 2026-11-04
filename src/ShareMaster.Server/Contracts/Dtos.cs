using System.ComponentModel.DataAnnotations;
using ShareMaster.Server.Domain;

namespace ShareMaster.Server.Contracts;

public sealed record AuthConfigDto(string Mode, string? ClientId, string? Authority, string[] Scopes, IReadOnlyList<DevUserDto> DevUsers);

public sealed record DevUserDto(string UserKey, string DisplayName);

public sealed record MeDto(string UserKey, string DisplayName, string? Email, bool IsSystemAdmin);

public sealed record UserDto(string UserKey, string DisplayName, string? Email);

public sealed record ManagerDto(string UserKey, string DisplayName, bool ReceiveNotifications);

public sealed record CalendarDto(
    int Id,
    string Code,
    string Name,
    string? Description,
    int? ParentId,
    int SortOrder,
    string? WeeklyHolidays,
    bool? UsePublicHolidays,
    int[] EffectiveWeeklyHolidays,
    bool EffectiveUsePublicHolidays,
    IReadOnlyList<ManagerDto> Managers,
    bool CanEdit,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);

public sealed class CalendarSaveRequest
{
    [Required, MaxLength(50), RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "コードは英数字・ハイフン・アンダースコアで入力してください。")]
    public string Code { get; set; } = "";
    [Required, MaxLength(100)]
    public string Name { get; set; } = "";
    [MaxLength(1000)]
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public int SortOrder { get; set; }
    /// <summary>定休曜日（0=日〜6=土）。null の場合は親を継承</summary>
    public int[]? WeeklyHolidays { get; set; }
    public bool? UsePublicHolidays { get; set; }
    public List<ManagerDto> Managers { get; set; } = [];
}

public sealed record ResolvedNoteDto(int CalendarId, string CalendarName, string Text);

public sealed record ResolvedDayDto(
    DateOnly Date,
    bool IsHoliday,
    string Source,
    int? DayTypeId,
    string? DayTypeName,
    string? Color,
    string? Name,
    int? SourceCalendarId,
    string? SourceCalendarName,
    bool HasOwnSetting,
    IReadOnlyList<ResolvedNoteDto> Notes)
{
    public static ResolvedDayDto From(ResolvedDay d) => new(
        d.Date, d.IsHoliday, d.Source.ToString(), d.DayTypeId, d.DayTypeName, d.Color, d.Name,
        d.SourceCalendarId, d.SourceCalendarName, d.HasOwnSetting,
        d.Notes.Select(n => new ResolvedNoteDto(n.CalendarId, n.CalendarName, n.Text)).ToList());
}

public sealed record CalendarDaysDto(int CalendarId, IReadOnlyList<ResolvedDayDto> Days);

public sealed record OwnSettingDto(int? DayTypeId, string? Name, string? Note, DateTimeOffset UpdatedAt, string UpdatedBy);

public sealed record DayDetailDto(
    int CalendarId,
    DateOnly Date,
    ResolvedDayDto Effective,
    ResolvedDayDto? Inherited,
    OwnSettingDto? Own,
    bool CanEdit);

public enum DayUpdateMode
{
    /// <summary>日別設定を削除し、親の設定に従う</summary>
    Inherit,
    /// <summary>日種別・名称・特記事項を設定する</summary>
    Set,
}

public sealed class DayUpdateRequest
{
    [Required]
    public DateOnly From { get; set; }
    /// <summary>期間指定の終了日（省略時は From のみ）</summary>
    public DateOnly? To { get; set; }
    /// <summary>期間指定時に対象とする曜日（省略時はすべて）</summary>
    public int[]? DaysOfWeek { get; set; }
    public DayUpdateMode Mode { get; set; } = DayUpdateMode.Set;
    public int? DayTypeId { get; set; }
    [MaxLength(100)]
    public string? Name { get; set; }
    [MaxLength(1000)]
    public string? Note { get; set; }
}

public sealed record DayUpdateResultDto(int UpdatedCount, int NotifiedUserCount);

public sealed record YearRangeDto(int MinYear, int MaxYear);

public sealed record DayTypeDto(int Id, string Code, string Name, string Color, bool IsHoliday, int SortOrder, bool IsSystem);

public sealed class DayTypeSaveRequest
{
    [Required, MaxLength(50), RegularExpression("^[A-Za-z0-9_-]+$")]
    public string Code { get; set; } = "";
    [Required, MaxLength(100)]
    public string Name { get; set; } = "";
    [Required, RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "色は #RRGGBB 形式で入力してください。")]
    public string Color { get; set; } = "#9E9E9E";
    public bool IsHoliday { get; set; } = true;
    public int SortOrder { get; set; }
}

public sealed record PublicHolidayDto(int Id, DateOnly Date, string Name);

public sealed class PublicHolidaySaveRequest
{
    [Required]
    public DateOnly Date { get; set; }
    [Required, MaxLength(100)]
    public string Name { get; set; } = "";
}

public sealed record GenerateResultDto(int Added, int Skipped);

public sealed record HistoryDto(
    long Id, int? CalendarId, string? CalendarName, string EntityType, string EntityKey, string Action,
    string Summary, string? BeforeJson, string? AfterJson, string ChangedBy, string ChangedByName, DateTimeOffset ChangedAt);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public sealed record NotificationDto(long Id, int? CalendarId, string Title, string Message, string? LinkUrl, bool IsRead, DateTimeOffset CreatedAt);

public sealed record NotificationSummaryDto(int UnreadCount, IReadOnlyList<NotificationDto> Items);

public sealed record SystemAdminDto(int Id, string UserKey, string DisplayName, DateTimeOffset CreatedAt, string CreatedBy);

public sealed class SystemAdminSaveRequest
{
    [Required, MaxLength(256)]
    public string UserKey { get; set; } = "";
    [MaxLength(200)]
    public string? DisplayName { get; set; }
}
