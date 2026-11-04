namespace ShareMaster.Server.Data.Entities;

/// <summary>作成・更新の監査情報を持つエンティティの基底クラス</summary>
public abstract class AuditableEntity
{
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = "";
}

/// <summary>カレンダー（親子関係を持ち、子は親の設定を上書きする）</summary>
public class Calendar : AuditableEntity
{
    public int Id { get; set; }
    /// <summary>外部連携用のコード（一意）</summary>
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public Calendar? Parent { get; set; }
    public List<Calendar> Children { get; set; } = [];
    public int SortOrder { get; set; }
    /// <summary>定休曜日（"0,6" 形式。0=日曜）。null の場合は親を継承</summary>
    public string? WeeklyHolidays { get; set; }
    /// <summary>祝日を休日として扱うか。null の場合は親を継承</summary>
    public bool? UsePublicHolidays { get; set; }
    /// <summary>論理削除フラグ（履歴保持のため物理削除しない）</summary>
    public bool IsActive { get; set; } = true;
    public List<CalendarManager> Managers { get; set; } = [];
    public List<CalendarDay> Days { get; set; } = [];
}

/// <summary>カレンダー管理者（編集権限と親カレンダー更新通知の対象）</summary>
public class CalendarManager
{
    public int Id { get; set; }
    public int CalendarId { get; set; }
    public Calendar? Calendar { get; set; }
    public string UserKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool ReceiveNotifications { get; set; } = true;
}

/// <summary>日種別（休日の種類と表示色）</summary>
public class DayType
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>表示色（#RRGGBB）</summary>
    public string Color { get; set; } = "#9E9E9E";
    public bool IsHoliday { get; set; } = true;
    public int SortOrder { get; set; }
    /// <summary>システム予約（削除不可）</summary>
    public bool IsSystem { get; set; }
}

/// <summary>カレンダーの日別設定（親の設定を上書きする差分のみを保持）</summary>
public class CalendarDay : AuditableEntity
{
    public int Id { get; set; }
    public int CalendarId { get; set; }
    public Calendar? Calendar { get; set; }
    public DateOnly Date { get; set; }
    /// <summary>日種別。null の場合は区分を上書きせず特記事項のみ</summary>
    public int? DayTypeId { get; set; }
    public DayType? DayType { get; set; }
    /// <summary>休日名など表示用の名称</summary>
    public string? Name { get; set; }
    /// <summary>特記事項</summary>
    public string? Note { get; set; }
}

/// <summary>祝日（全カレンダー共通）</summary>
public class PublicHoliday
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Name { get; set; } = "";
}

/// <summary>利用者（ログイン時に自動登録。管理者選択の候補に使用）</summary>
public class AppUser
{
    public string UserKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Email { get; set; }
    public DateTimeOffset LastAccessedAt { get; set; }
}

/// <summary>システム管理者（カレンダーの新規追加などが可能）</summary>
public class SystemAdmin
{
    public int Id { get; set; }
    public string UserKey { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "";
}

/// <summary>変更履歴</summary>
public class ChangeHistory
{
    public long Id { get; set; }
    public int? CalendarId { get; set; }
    /// <summary>対象種別（Calendar / CalendarDay / DayType / PublicHoliday / SystemAdmin）</summary>
    public string EntityType { get; set; } = "";
    public string EntityKey { get; set; } = "";
    /// <summary>操作（Create / Update / Delete）</summary>
    public string Action { get; set; } = "";
    public string Summary { get; set; } = "";
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string ChangedBy { get; set; } = "";
    public string ChangedByName { get; set; } = "";
    public DateTimeOffset ChangedAt { get; set; }
}

/// <summary>アプリ内通知</summary>
public class Notification
{
    public long Id { get; set; }
    public string UserKey { get; set; } = "";
    public int? CalendarId { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
