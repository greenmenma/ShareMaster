using ShareMaster.Server.Domain;

namespace ShareMaster.Server.Tests;

public class CalendarResolverTests
{
    // 本社(1) > 滋賀(2) > システムA(3)
    private static readonly CalendarNode[] Calendars =
    [
        new(1, "本社", null, null, null),
        new(2, "滋賀", 1, null, null),
        new(3, "システムA", 2, null, null),
        new(4, "システムC", 1, "0", false),
    ];

    private static readonly DayTypeInfo[] DayTypes =
    [
        new(1, "WORKDAY", "稼働日", "#607D8B", false),
        new(2, "COMPANY", "会社休日", "#2E7D32", true),
    ];

    private static readonly (DateOnly, string)[] Publics = [(new DateOnly(2026, 11, 3), "文化の日")];

    private static CalendarResolver Create(params DayOverride[] overrides) => new(Calendars, DayTypes, Publics, overrides);

    [Fact]
    public void 既定では土日と祝日が休み()
    {
        var days = Create().Resolve(3, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 7)).ToDictionary(x => x.Date);
        Assert.True(days[new DateOnly(2026, 11, 1)].IsHoliday); // 日
        Assert.Equal(DaySource.Weekly, days[new DateOnly(2026, 11, 1)].Source);
        Assert.False(days[new DateOnly(2026, 11, 2)].IsHoliday); // 月
        Assert.Equal(DaySource.PublicHoliday, days[new DateOnly(2026, 11, 3)].Source);
        Assert.Equal("文化の日", days[new DateOnly(2026, 11, 3)].Name);
        Assert.True(days[new DateOnly(2026, 11, 7)].IsHoliday); // 土
    }

    [Fact]
    public void 親の休日設定が子孫に継承される()
    {
        var r = Create(new DayOverride(1, new DateOnly(2026, 11, 2), 2, "創立記念日", null));
        var day = r.Resolve(3, new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 2)).Single();
        Assert.True(day.IsHoliday);
        Assert.Equal("創立記念日", day.Name);
        Assert.Equal(1, day.SourceCalendarId);
        Assert.False(day.HasOwnSetting);
    }

    [Fact]
    public void 子の設定が親の設定を上書きする()
    {
        var r = Create(
            new DayOverride(1, new DateOnly(2026, 11, 2), 2, null, "本社は休み"),
            new DayOverride(2, new DateOnly(2026, 11, 2), 1, null, "滋賀は出勤"));
        var shiga = r.Resolve(2, new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 2)).Single();
        Assert.False(shiga.IsHoliday);
        Assert.Equal(2, shiga.SourceCalendarId);
        Assert.True(shiga.HasOwnSetting);
        Assert.Equal(["本社は休み", "滋賀は出勤"], shiga.Notes.Select(x => x.Text));

        // 孫も滋賀の設定を引き継ぐ
        Assert.False(r.Resolve(3, new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 2)).Single().IsHoliday);
        // 本社自身は休み
        Assert.True(r.Resolve(1, new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 2)).Single().IsHoliday);
    }

    [Fact]
    public void 定休曜日と祝日使用有無はカレンダーごとに上書きできる()
    {
        var days = Create().Resolve(4, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 7)).ToDictionary(x => x.Date);
        Assert.True(days[new DateOnly(2026, 11, 1)].IsHoliday);  // 日曜のみ定休
        Assert.False(days[new DateOnly(2026, 11, 3)].IsHoliday); // 祝日を使わない
        Assert.False(days[new DateOnly(2026, 11, 7)].IsHoliday); // 土曜は稼働
    }

    [Fact]
    public void 特記事項のみの設定は区分を変えない()
    {
        var r = Create(new DayOverride(3, new DateOnly(2026, 11, 2), null, null, "棚卸"));
        var day = r.Resolve(3, new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 2)).Single();
        Assert.False(day.IsHoliday);
        Assert.Equal(DaySource.Default, day.Source);
        Assert.Equal("棚卸", Assert.Single(day.Notes).Text);
    }

    [Fact]
    public void 子孫を取得できる()
    {
        Assert.Equal([2, 3, 4], Create().GetDescendants(1).Order());
        Assert.Equal([3], Create().GetDescendants(2));
    }
}
