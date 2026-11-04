namespace ShareMaster.Server.Domain;

/// <summary>解決処理に必要なカレンダー情報</summary>
public sealed record CalendarNode(int Id, string Name, int? ParentId, string? WeeklyHolidays, bool? UsePublicHolidays);

/// <summary>解決処理に必要な日種別情報</summary>
public sealed record DayTypeInfo(int Id, string Code, string Name, string Color, bool IsHoliday);

/// <summary>解決処理に必要な日別設定（上書き）</summary>
public sealed record DayOverride(int CalendarId, DateOnly Date, int? DayTypeId, string? Name, string? Note);

/// <summary>特記事項（どのカレンダーで設定されたものか）</summary>
public sealed record ResolvedNote(int CalendarId, string CalendarName, string Text);

/// <summary>休日区分の決定根拠</summary>
public enum DaySource
{
    /// <summary>平日（既定）</summary>
    Default,
    /// <summary>定休曜日</summary>
    Weekly,
    /// <summary>祝日</summary>
    PublicHoliday,
    /// <summary>日別設定による上書き</summary>
    Override,
}

/// <summary>親子関係を考慮して確定した 1 日分の情報</summary>
public sealed record ResolvedDay(
    DateOnly Date,
    bool IsHoliday,
    DaySource Source,
    int? DayTypeId,
    string? DayTypeName,
    string? Color,
    string? Name,
    int? SourceCalendarId,
    string? SourceCalendarName,
    bool HasOwnSetting,
    IReadOnlyList<ResolvedNote> Notes);

/// <summary>
/// カレンダーの親子関係を辿り、日ごとの休日区分を確定する。
/// 優先順位：祖先から子孫へ順に日別設定で上書き（子が最優先）＞ 祝日 ＞ 定休曜日。
/// 定休曜日・祝日使用有無は最も近い祖先の設定を継承する。
/// </summary>
public sealed class CalendarResolver
{
    public const string DefaultWeeklyHolidays = "0,6";

    private readonly Dictionary<int, CalendarNode> _calendars;
    private readonly Dictionary<int, DayTypeInfo> _dayTypes;
    private readonly Dictionary<DateOnly, string> _publicHolidays;
    private readonly Dictionary<(int, DateOnly), DayOverride> _overrides;

    public CalendarResolver(
        IEnumerable<CalendarNode> calendars,
        IEnumerable<DayTypeInfo> dayTypes,
        IEnumerable<(DateOnly Date, string Name)> publicHolidays,
        IEnumerable<DayOverride> overrides)
    {
        _calendars = calendars.ToDictionary(x => x.Id);
        _dayTypes = dayTypes.ToDictionary(x => x.Id);
        _publicHolidays = publicHolidays.ToDictionary(x => x.Date, x => x.Name);
        _overrides = overrides.ToDictionary(x => (x.CalendarId, x.Date));
    }

    /// <summary>ルートから対象カレンダーまでの経路を返す（循環参照は検出して打ち切る）</summary>
    public IReadOnlyList<CalendarNode> GetLineage(int calendarId)
    {
        var path = new List<CalendarNode>();
        var visited = new HashSet<int>();
        int? current = calendarId;
        while (current is int id && _calendars.TryGetValue(id, out var node) && visited.Add(id))
        {
            path.Add(node);
            current = node.ParentId;
        }
        path.Reverse();
        return path;
    }

    public IReadOnlySet<DayOfWeek> GetEffectiveWeeklyHolidays(int calendarId)
    {
        var lineage = GetLineage(calendarId);
        var setting = lineage.Select(x => x.WeeklyHolidays).LastOrDefault(x => x is not null) ?? DefaultWeeklyHolidays;
        return ParseWeekly(setting);
    }

    public bool GetEffectiveUsePublicHolidays(int calendarId) =>
        GetLineage(calendarId).Select(x => x.UsePublicHolidays).LastOrDefault(x => x is not null) ?? true;

    public static IReadOnlySet<DayOfWeek> ParseWeekly(string? value) =>
        (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => int.TryParse(x, out var n) && n is >= 0 and <= 6 ? (DayOfWeek?)n : null)
            .Where(x => x is not null)
            .Select(x => x!.Value)
            .ToHashSet();

    public IReadOnlyList<ResolvedDay> Resolve(int calendarId, DateOnly from, DateOnly to)
    {
        var lineage = GetLineage(calendarId);
        if (lineage.Count == 0) return [];
        var weekly = ParseWeekly(lineage.Select(x => x.WeeklyHolidays).LastOrDefault(x => x is not null) ?? DefaultWeeklyHolidays);
        var usePublic = lineage.Select(x => x.UsePublicHolidays).LastOrDefault(x => x is not null) ?? true;

        var result = new List<ResolvedDay>();
        for (var date = from; date <= to; date = date.AddDays(1))
            result.Add(ResolveDay(lineage, weekly, usePublic, calendarId, date));
        return result;
    }

    private ResolvedDay ResolveDay(IReadOnlyList<CalendarNode> lineage, IReadOnlySet<DayOfWeek> weekly, bool usePublic, int calendarId, DateOnly date)
    {
        var isHoliday = false;
        var source = DaySource.Default;
        int? dayTypeId = null;
        string? dayTypeName = null, color = null, name = null, sourceCalendarName = null;
        int? sourceCalendarId = null;

        if (usePublic && _publicHolidays.TryGetValue(date, out var holidayName))
        {
            isHoliday = true;
            source = DaySource.PublicHoliday;
            name = holidayName;
            dayTypeName = "祝日";
        }
        else if (weekly.Contains(date.DayOfWeek))
        {
            isHoliday = true;
            source = DaySource.Weekly;
            dayTypeName = "定休日";
        }

        var notes = new List<ResolvedNote>();
        var hasOwn = false;
        foreach (var node in lineage)
        {
            if (!_overrides.TryGetValue((node.Id, date), out var ov)) continue;
            if (node.Id == calendarId) hasOwn = true;
            if (!string.IsNullOrWhiteSpace(ov.Note)) notes.Add(new ResolvedNote(node.Id, node.Name, ov.Note!));
            if (ov.DayTypeId is int typeId && _dayTypes.TryGetValue(typeId, out var type))
            {
                isHoliday = type.IsHoliday;
                source = DaySource.Override;
                dayTypeId = type.Id;
                dayTypeName = type.Name;
                color = type.Color;
                name = string.IsNullOrWhiteSpace(ov.Name) ? (type.IsHoliday ? type.Name : null) : ov.Name;
                sourceCalendarId = node.Id;
                sourceCalendarName = node.Name;
            }
            else if (!string.IsNullOrWhiteSpace(ov.Name))
            {
                // 区分は変えず名称のみ上書き
                name = ov.Name;
            }
        }

        return new ResolvedDay(date, isHoliday, source, dayTypeId, dayTypeName, color, name,
            sourceCalendarId, sourceCalendarName, hasOwn, notes);
    }

    /// <summary>指定カレンダーの子孫（自身を含まない）を返す</summary>
    public IReadOnlyList<int> GetDescendants(int calendarId)
    {
        var children = _calendars.Values.ToLookup(x => x.ParentId);
        var result = new List<int>();
        var stack = new Stack<int>([calendarId]);
        var visited = new HashSet<int> { calendarId };
        while (stack.Count > 0)
        {
            foreach (var c in children[stack.Pop()])
            {
                if (!visited.Add(c.Id)) continue;
                result.Add(c.Id);
                stack.Push(c.Id);
            }
        }
        return result;
    }
}
