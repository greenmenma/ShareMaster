using ShareMaster.Server.Domain;

namespace ShareMaster.Server.Tests;

public class JapaneseHolidayCalculatorTests
{
    [Fact]
    public void 年2026の祝日が内閣府公表どおりに算出される()
    {
        var expected = new[]
        {
            "2026-01-01", "2026-01-12", "2026-02-11", "2026-02-23", "2026-03-20", "2026-04-29",
            "2026-05-03", "2026-05-04", "2026-05-05", "2026-05-06", "2026-07-20", "2026-08-11",
            "2026-09-21", "2026-09-22", "2026-09-23", "2026-10-12", "2026-11-03", "2026-11-23",
        };
        var actual = JapaneseHolidayCalculator.GetHolidays(2026).Select(x => x.Date.ToString("yyyy-MM-dd")).ToArray();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 年2026の振替休日と国民の休日の名称()
    {
        var h = JapaneseHolidayCalculator.GetHolidays(2026).ToDictionary(x => x.Date, x => x.Name);
        Assert.Equal("振替休日", h[new DateOnly(2026, 5, 6)]);
        Assert.Equal("国民の休日", h[new DateOnly(2026, 9, 22)]);
    }

    [Fact]
    public void 年2025の祝日数と振替休日()
    {
        var h = JapaneseHolidayCalculator.GetHolidays(2025).ToDictionary(x => x.Date, x => x.Name);
        Assert.Equal(19, h.Count);
        Assert.Equal("振替休日", h[new DateOnly(2025, 2, 24)]);
        Assert.Equal("振替休日", h[new DateOnly(2025, 5, 6)]);
        Assert.Equal("振替休日", h[new DateOnly(2025, 11, 24)]);
    }

    [Fact]
    public void 東京五輪特例の年2021()
    {
        var h = JapaneseHolidayCalculator.GetHolidays(2021).ToDictionary(x => x.Date, x => x.Name);
        Assert.Equal("海の日", h[new DateOnly(2021, 7, 22)]);
        Assert.Equal("スポーツの日", h[new DateOnly(2021, 7, 23)]);
        Assert.Equal("山の日", h[new DateOnly(2021, 8, 8)]);
        Assert.Equal("振替休日", h[new DateOnly(2021, 8, 9)]);
    }

    [Fact]
    public void 範囲外の年は例外()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => JapaneseHolidayCalculator.GetHolidays(1999));
    }
}
