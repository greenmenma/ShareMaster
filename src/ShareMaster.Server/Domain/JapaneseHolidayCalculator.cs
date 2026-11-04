namespace ShareMaster.Server.Domain;

/// <summary>
/// 日本の祝日を「国民の祝日に関する法律」に基づいて算出する。
/// 2000〜2099 年を対象とし、振替休日・国民の休日・特例年（2019〜2021）に対応する。
/// 将来の法改正は祝日マスタ画面での手動修正で対応する想定。
/// </summary>
public static class JapaneseHolidayCalculator
{
    public static IReadOnlyList<(DateOnly Date, string Name)> GetHolidays(int year)
    {
        if (year < 2000 || year > 2099)
            throw new ArgumentOutOfRangeException(nameof(year), "対象は 2000〜2099 年です。");

        var h = new SortedDictionary<DateOnly, string>();
        void Add(int m, int d, string name) => h[new DateOnly(year, m, d)] = name;

        Add(1, 1, "元日");
        h[NthMonday(year, 1, 2)] = "成人の日";
        Add(2, 11, "建国記念の日");
        if (year >= 2020) Add(2, 23, "天皇誕生日");
        Add(3, VernalEquinoxDay(year), "春分の日");
        Add(4, 29, year >= 2007 ? "昭和の日" : "みどりの日");
        Add(5, 3, "憲法記念日");
        if (year >= 2007) Add(5, 4, "みどりの日");
        Add(5, 5, "こどもの日");

        // 海の日
        if (year == 2020) Add(7, 23, "海の日");
        else if (year == 2021) Add(7, 22, "海の日");
        else if (year >= 2003) h[NthMonday(year, 7, 3)] = "海の日";
        else Add(7, 20, "海の日");

        // 山の日
        if (year == 2020) Add(8, 10, "山の日");
        else if (year == 2021) Add(8, 8, "山の日");
        else if (year >= 2016) Add(8, 11, "山の日");

        // 敬老の日
        if (year >= 2003) h[NthMonday(year, 9, 3)] = "敬老の日";
        else Add(9, 15, "敬老の日");

        Add(9, AutumnalEquinoxDay(year), "秋分の日");

        // スポーツの日（2019 年以前は体育の日）
        if (year == 2020) Add(7, 24, "スポーツの日");
        else if (year == 2021) Add(7, 23, "スポーツの日");
        else h[NthMonday(year, 10, 2)] = year >= 2020 ? "スポーツの日" : "体育の日";

        Add(11, 3, "文化の日");
        Add(11, 23, "勤労感謝の日");
        if (year <= 2018) Add(12, 23, "天皇誕生日");

        // 2019 年の特例
        if (year == 2019)
        {
            Add(5, 1, "休日（即位の日）");
            Add(10, 22, "休日（即位礼正殿の儀の行われる日）");
        }

        // 国民の休日：前日と翌日が祝日である平日
        var baseDays = h.Keys.ToHashSet();
        foreach (var d in baseDays.ToList())
        {
            var next = d.AddDays(1);
            var nextNext = d.AddDays(2);
            if (!baseDays.Contains(next) && baseDays.Contains(nextNext) && next.DayOfWeek != DayOfWeek.Sunday)
                h[next] = "国民の休日";
        }

        // 振替休日：祝日が日曜日の場合、その後の最初の祝日でない日
        foreach (var d in h.Keys.Where(x => x.DayOfWeek == DayOfWeek.Sunday).ToList())
        {
            var sub = d.AddDays(1);
            while (h.ContainsKey(sub)) sub = sub.AddDays(1);
            h[sub] = "振替休日";
        }

        return h.Select(x => (x.Key, x.Value)).ToList();
    }

    private static DateOnly NthMonday(int year, int month, int nth)
    {
        var first = new DateOnly(year, month, 1);
        var offset = ((int)DayOfWeek.Monday - (int)first.DayOfWeek + 7) % 7;
        return first.AddDays(offset + 7 * (nth - 1));
    }

    private static int VernalEquinoxDay(int year) =>
        (int)Math.Floor(20.8431 + 0.242194 * (year - 1980) - Math.Floor((year - 1980) / 4.0));

    private static int AutumnalEquinoxDay(int year) =>
        (int)Math.Floor(23.2488 + 0.242194 * (year - 1980) - Math.Floor((year - 1980) / 4.0));
}
