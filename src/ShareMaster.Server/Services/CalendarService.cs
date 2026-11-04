using Microsoft.EntityFrameworkCore;
using ShareMaster.Server.Auth;
using ShareMaster.Server.Contracts;
using ShareMaster.Server.Data;
using ShareMaster.Server.Data.Entities;
using ShareMaster.Server.Domain;

namespace ShareMaster.Server.Services;

public sealed class CalendarService(
    ShareMasterDbContext db,
    ICurrentUser user,
    HistoryWriter history,
    NotificationService notifications,
    TimeProvider clock)
{
    /// <summary>一度に取得できる最大日数（すべて表示でも過大な応答にならないよう制限）</summary>
    public const int MaxRangeDays = 366 * 10;

    private static readonly string[] WeekdayNames = ["日", "月", "火", "水", "木", "金", "土"];

    public async Task<IReadOnlyList<CalendarDto>> GetAllAsync(CancellationToken ct)
    {
        var calendars = await db.Calendars.AsNoTracking().Where(x => x.IsActive)
            .Include(x => x.Managers)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync(ct);
        var resolver = new CalendarResolver(calendars.Select(ToNode), [], [], []);
        var result = new List<CalendarDto>();
        foreach (var c in calendars)
            result.Add(ToDto(c, resolver, await user.CanEditCalendarAsync(c.Id, ct)));
        return result;
    }

    public async Task<CalendarDto> GetAsync(int id, CancellationToken ct)
    {
        var calendars = await db.Calendars.AsNoTracking().Where(x => x.IsActive).Include(x => x.Managers).ToListAsync(ct);
        var c = calendars.FirstOrDefault(x => x.Id == id) ?? throw ServiceException.NotFound("カレンダーが見つかりません。");
        var resolver = new CalendarResolver(calendars.Select(ToNode), [], [], []);
        return ToDto(c, resolver, await user.CanEditCalendarAsync(id, ct));
    }

    public async Task<CalendarDto> CreateAsync(CalendarSaveRequest req, CancellationToken ct)
    {
        if (!await user.IsSystemAdminAsync(ct)) throw ServiceException.Forbidden("カレンダーの新規追加はシステム管理者のみ可能です。");
        await ValidateAsync(null, req, ct);

        var now = clock.GetUtcNow();
        var entity = new Calendar
        {
            CreatedAt = now, CreatedBy = user.UserKey, UpdatedAt = now, UpdatedBy = user.UserKey,
        };
        Apply(entity, req);
        db.Calendars.Add(entity);
        await db.SaveChangesAsync(ct);

        history.Add(nameof(Calendar), entity.Id.ToString(), "Create", $"カレンダー「{entity.Name}」を作成", null, Snapshot(entity), entity.Id);
        await db.SaveChangesAsync(ct);
        return await GetAsync(entity.Id, ct);
    }

    public async Task<CalendarDto> UpdateAsync(int id, CalendarSaveRequest req, CancellationToken ct)
    {
        var entity = await db.Calendars.Include(x => x.Managers).FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct)
                     ?? throw ServiceException.NotFound("カレンダーが見つかりません。");
        var isAdmin = await user.IsSystemAdminAsync(ct);
        if (!isAdmin && !await user.CanEditCalendarAsync(id, ct)) throw ServiceException.Forbidden();
        // 階層構造・コードの変更はシステム管理者のみ
        if (!isAdmin && (entity.ParentId != req.ParentId || entity.Code != req.Code))
            throw ServiceException.Forbidden("親カレンダー・コードの変更はシステム管理者のみ可能です。");
        await ValidateAsync(id, req, ct);

        var before = Snapshot(entity);
        var weeklyBefore = entity.WeeklyHolidays;
        var publicBefore = entity.UsePublicHolidays;
        Apply(entity, req);
        entity.UpdatedAt = clock.GetUtcNow();
        entity.UpdatedBy = user.UserKey;

        history.Add(nameof(Calendar), id.ToString(), "Update", $"カレンダー「{entity.Name}」の設定を変更", before, Snapshot(entity), id);
        if (weeklyBefore != entity.WeeklyHolidays || publicBefore != entity.UsePublicHolidays)
            await notifications.NotifyDescendantManagersAsync(entity, "定休曜日または祝日の扱いが変更されました", ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        if (!await user.IsSystemAdminAsync(ct)) throw ServiceException.Forbidden("カレンダーの削除はシステム管理者のみ可能です。");
        var entity = await db.Calendars.Include(x => x.Managers).FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct)
                     ?? throw ServiceException.NotFound("カレンダーが見つかりません。");
        if (await db.Calendars.AnyAsync(x => x.ParentId == id && x.IsActive, ct))
            throw ServiceException.Conflict("子カレンダーが存在するため削除できません。");

        var before = Snapshot(entity);
        entity.IsActive = false;
        // 論理削除後も同じコードを再利用できるようコードを退避する
        entity.Code = $"{entity.Code}~{entity.Id}";
        entity.UpdatedAt = clock.GetUtcNow();
        entity.UpdatedBy = user.UserKey;
        history.Add(nameof(Calendar), id.ToString(), "Delete", $"カレンダー「{entity.Name}」を削除", before, null, id);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>指定カレンダー群の期間内の確定済み日情報を取得する</summary>
    public async Task<IReadOnlyList<CalendarDaysDto>> GetDaysAsync(IReadOnlyCollection<int>? calendarIds, DateOnly from, DateOnly to, CancellationToken ct)
    {
        ValidateRange(from, to);
        var resolver = await BuildResolverAsync(from, to, ct);
        var targets = calendarIds is { Count: > 0 }
            ? calendarIds
            : await db.Calendars.Where(x => x.IsActive).OrderBy(x => x.SortOrder).Select(x => x.Id).ToListAsync(ct);
        return targets
            .Select(id => new CalendarDaysDto(id, resolver.Resolve(id, from, to).Select(ResolvedDayDto.From).ToList()))
            .Where(x => x.Days.Count > 0)
            .ToList();
    }

    public async Task<DayDetailDto> GetDayDetailAsync(int calendarId, DateOnly date, CancellationToken ct)
    {
        var calendar = await db.Calendars.AsNoTracking().FirstOrDefaultAsync(x => x.Id == calendarId && x.IsActive, ct)
                       ?? throw ServiceException.NotFound("カレンダーが見つかりません。");
        var resolver = await BuildResolverAsync(date, date, ct);
        var effective = resolver.Resolve(calendarId, date, date).Single();
        var inherited = calendar.ParentId is int parentId ? resolver.Resolve(parentId, date, date).SingleOrDefault() : null;
        var own = await db.CalendarDays.AsNoTracking().FirstOrDefaultAsync(x => x.CalendarId == calendarId && x.Date == date, ct);
        return new DayDetailDto(
            calendarId, date,
            ResolvedDayDto.From(effective),
            inherited is null ? null : ResolvedDayDto.From(inherited),
            own is null ? null : new OwnSettingDto(own.DayTypeId, own.Name, own.Note, own.UpdatedAt, own.UpdatedBy),
            await user.CanEditCalendarAsync(calendarId, ct));
    }

    /// <summary>日別設定を更新する（単日・期間一括）。子孫カレンダーの管理者へ通知する</summary>
    public async Task<DayUpdateResultDto> UpdateDaysAsync(int calendarId, DayUpdateRequest req, CancellationToken ct)
    {
        var calendar = await db.Calendars.FirstOrDefaultAsync(x => x.Id == calendarId && x.IsActive, ct)
                       ?? throw ServiceException.NotFound("カレンダーが見つかりません。");
        if (!await user.CanEditCalendarAsync(calendarId, ct)) throw ServiceException.Forbidden("このカレンダーを編集する権限がありません。");

        var to = req.To ?? req.From;
        if (to < req.From) throw ServiceException.BadRequest("終了日は開始日以降を指定してください。");
        if (to.DayNumber - req.From.DayNumber > 366) throw ServiceException.BadRequest("一括設定できる期間は 1 年以内です。");

        DayType? dayType = null;
        if (req.Mode == DayUpdateMode.Set)
        {
            if (req.DayTypeId is int typeId)
                dayType = await db.DayTypes.FirstOrDefaultAsync(x => x.Id == typeId, ct) ?? throw ServiceException.BadRequest("日種別が不正です。");
            else if (string.IsNullOrWhiteSpace(req.Name) && string.IsNullOrWhiteSpace(req.Note))
                throw ServiceException.BadRequest("日種別・名称・特記事項のいずれかを入力してください。");
        }

        var weekdays = req.DaysOfWeek is { Length: > 0 } ? req.DaysOfWeek.Select(x => (DayOfWeek)x).ToHashSet() : null;
        var dates = Enumerable.Range(0, to.DayNumber - req.From.DayNumber + 1)
            .Select(req.From.AddDays)
            .Where(d => weekdays is null || weekdays.Contains(d.DayOfWeek))
            .ToList();
        if (dates.Count == 0) throw ServiceException.BadRequest("対象となる日付がありません。");

        var existing = await db.CalendarDays
            .Where(x => x.CalendarId == calendarId && x.Date >= req.From && x.Date <= to)
            .ToDictionaryAsync(x => x.Date, ct);
        var typeNames = await db.DayTypes.ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        var now = clock.GetUtcNow();
        var changed = 0;

        foreach (var date in dates)
        {
            existing.TryGetValue(date, out var row);
            var before = row is null ? null : DaySnapshot(row, typeNames);
            if (req.Mode == DayUpdateMode.Inherit)
            {
                if (row is null) continue;
                db.CalendarDays.Remove(row);
                history.Add(nameof(CalendarDay), $"{calendarId}:{date:yyyy-MM-dd}", "Delete",
                    $"{date:yyyy/MM/dd} の個別設定を削除（親の設定に従う）", before, null, calendarId);
            }
            else
            {
                var name = string.IsNullOrWhiteSpace(req.Name) ? null : req.Name.Trim();
                var note = string.IsNullOrWhiteSpace(req.Note) ? null : req.Note.Trim();
                if (row is not null && row.DayTypeId == dayType?.Id && row.Name == name && row.Note == note) continue;
                var action = row is null ? "Create" : "Update";
                if (row is null)
                {
                    row = new CalendarDay { CalendarId = calendarId, Date = date, CreatedAt = now, CreatedBy = user.UserKey };
                    db.CalendarDays.Add(row);
                }
                row.DayTypeId = dayType?.Id;
                row.Name = name;
                row.Note = note;
                row.UpdatedAt = now;
                row.UpdatedBy = user.UserKey;
                var label = dayType?.Name ?? "特記事項";
                history.Add(nameof(CalendarDay), $"{calendarId}:{date:yyyy-MM-dd}", action,
                    $"{date:yyyy/MM/dd} を「{label}{(name is null ? "" : $"：{name}")}」に設定", before, DaySnapshot(row, typeNames), calendarId);
            }
            changed++;
        }

        var notified = 0;
        if (changed > 0)
        {
            var period = dates.Count == 1 ? $"{dates[0]:yyyy/MM/dd}" : $"{dates[0]:yyyy/MM/dd}〜{dates[^1]:yyyy/MM/dd}（{changed} 日）";
            var what = req.Mode == DayUpdateMode.Inherit ? "の個別設定が削除されました" : $"が「{dayType?.Name ?? "特記事項"}」に設定されました";
            notified = await notifications.NotifyDescendantManagersAsync(calendar, $"{period}{what}", ct);
        }
        await db.SaveChangesAsync(ct);
        return new DayUpdateResultDto(changed, notified);
    }

    /// <summary>データが存在する年の範囲（「すべて」表示用）</summary>
    public async Task<YearRangeDto> GetYearRangeAsync(CancellationToken ct)
    {
        var thisYear = clock.GetLocalNow().Year;
        var dayMin = await db.CalendarDays.Select(x => (DateOnly?)x.Date).MinAsync(ct);
        var dayMax = await db.CalendarDays.Select(x => (DateOnly?)x.Date).MaxAsync(ct);
        var phMin = await db.PublicHolidays.Select(x => (DateOnly?)x.Date).MinAsync(ct);
        var phMax = await db.PublicHolidays.Select(x => (DateOnly?)x.Date).MaxAsync(ct);
        var min = new[] { dayMin?.Year, phMin?.Year, thisYear }.Where(x => x.HasValue).Min()!.Value;
        var max = new[] { dayMax?.Year, phMax?.Year, thisYear }.Where(x => x.HasValue).Max()!.Value;
        // 過大な範囲にならないよう 10 年に制限する
        if (max - min >= 10) min = max - 9;
        return new YearRangeDto(min, max);
    }

    private async Task<CalendarResolver> BuildResolverAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var nodes = await db.Calendars.AsNoTracking().Where(x => x.IsActive)
            .Select(x => new CalendarNode(x.Id, x.Name, x.ParentId, x.WeeklyHolidays, x.UsePublicHolidays)).ToListAsync(ct);
        var types = await db.DayTypes.AsNoTracking()
            .Select(x => new DayTypeInfo(x.Id, x.Code, x.Name, x.Color, x.IsHoliday)).ToListAsync(ct);
        var publics = await db.PublicHolidays.AsNoTracking().Where(x => x.Date >= from && x.Date <= to)
            .Select(x => new { x.Date, x.Name }).ToListAsync(ct);
        var overrides = await db.CalendarDays.AsNoTracking().Where(x => x.Date >= from && x.Date <= to)
            .Select(x => new DayOverride(x.CalendarId, x.Date, x.DayTypeId, x.Name, x.Note)).ToListAsync(ct);
        return new CalendarResolver(nodes, types, publics.Select(x => (x.Date, x.Name)), overrides);
    }

    private static void ValidateRange(DateOnly from, DateOnly to)
    {
        if (to < from) throw ServiceException.BadRequest("終了日は開始日以降を指定してください。");
        if (to.DayNumber - from.DayNumber > MaxRangeDays) throw ServiceException.BadRequest("取得期間が長すぎます。");
    }

    private async Task ValidateAsync(int? id, CalendarSaveRequest req, CancellationToken ct)
    {
        if (await db.Calendars.AnyAsync(x => x.Code == req.Code && x.Id != id, ct))
            throw ServiceException.Conflict($"コード「{req.Code}」は既に使用されています。");
        if (req.ParentId is int parentId)
        {
            if (!await db.Calendars.AnyAsync(x => x.Id == parentId && x.IsActive, ct))
                throw ServiceException.BadRequest("親カレンダーが存在しません。");
            if (id is int selfId)
            {
                // 自分自身や子孫を親に指定すると循環するため禁止
                var nodes = await db.Calendars.Where(x => x.IsActive)
                    .Select(x => new CalendarNode(x.Id, x.Name, x.ParentId, null, null)).ToListAsync(ct);
                var descendants = new CalendarResolver(nodes, [], [], []).GetDescendants(selfId);
                if (parentId == selfId || descendants.Contains(parentId))
                    throw ServiceException.BadRequest("自分自身または子孫カレンダーを親に指定することはできません。");
            }
        }
        if (req.WeeklyHolidays?.Any(x => x is < 0 or > 6) == true)
            throw ServiceException.BadRequest("定休曜日が不正です。");
    }

    private static void Apply(Calendar entity, CalendarSaveRequest req)
    {
        entity.Code = req.Code.Trim();
        entity.Name = req.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
        entity.ParentId = req.ParentId;
        entity.SortOrder = req.SortOrder;
        entity.WeeklyHolidays = req.WeeklyHolidays is null ? null : string.Join(",", req.WeeklyHolidays.Distinct().Order());
        entity.UsePublicHolidays = req.UsePublicHolidays;

        // 管理者の差分更新
        var requested = req.Managers
            .Where(x => !string.IsNullOrWhiteSpace(x.UserKey))
            .GroupBy(x => x.UserKey.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());
        entity.Managers.RemoveAll(m => !requested.ContainsKey(m.UserKey));
        foreach (var (key, m) in requested)
        {
            var existing = entity.Managers.FirstOrDefault(x => x.UserKey == key);
            if (existing is null)
            {
                entity.Managers.Add(new CalendarManager { UserKey = key, DisplayName = string.IsNullOrWhiteSpace(m.DisplayName) ? key : m.DisplayName, ReceiveNotifications = m.ReceiveNotifications });
            }
            else
            {
                existing.DisplayName = string.IsNullOrWhiteSpace(m.DisplayName) ? key : m.DisplayName;
                existing.ReceiveNotifications = m.ReceiveNotifications;
            }
        }
    }

    private static CalendarNode ToNode(Calendar c) => new(c.Id, c.Name, c.ParentId, c.WeeklyHolidays, c.UsePublicHolidays);

    private static CalendarDto ToDto(Calendar c, CalendarResolver resolver, bool canEdit) => new(
        c.Id, c.Code, c.Name, c.Description, c.ParentId, c.SortOrder,
        c.WeeklyHolidays, c.UsePublicHolidays,
        resolver.GetEffectiveWeeklyHolidays(c.Id).Select(x => (int)x).Order().ToArray(),
        resolver.GetEffectiveUsePublicHolidays(c.Id),
        c.Managers.OrderBy(x => x.Id).Select(x => new ManagerDto(x.UserKey, x.DisplayName, x.ReceiveNotifications)).ToList(),
        canEdit, c.UpdatedAt, c.UpdatedBy);

    private static object Snapshot(Calendar c) => new
    {
        c.Code, c.Name, c.Description, c.ParentId, c.SortOrder,
        WeeklyHolidays = c.WeeklyHolidays is null ? "（継承）" : string.Join("", CalendarResolver.ParseWeekly(c.WeeklyHolidays).Select(x => WeekdayNames[(int)x])),
        UsePublicHolidays = c.UsePublicHolidays switch { null => "（継承）", true => "休日扱い", false => "稼働日扱い" },
        Managers = c.Managers.Select(x => x.DisplayName).ToArray(),
    };

    private static object DaySnapshot(CalendarDay d, Dictionary<int, string> typeNames) => new
    {
        Date = d.Date.ToString("yyyy-MM-dd"),
        DayType = d.DayTypeId is int id ? typeNames.GetValueOrDefault(id) : null,
        d.Name,
        d.Note,
    };
}
