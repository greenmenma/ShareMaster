using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShareMaster.Server.Auth;
using ShareMaster.Server.Controllers;
using ShareMaster.Server.Data.Entities;

namespace ShareMaster.Server.Data;

/// <summary>起動時のマイグレーション適用と初期データ投入</summary>
public static class DbInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var dbSettings = sp.GetRequiredService<IOptions<DatabaseSettings>>().Value;
        var authSettings = sp.GetRequiredService<IOptions<AuthSettings>>().Value;
        var db = sp.GetRequiredService<ShareMasterDbContext>();
        var clock = sp.GetRequiredService<TimeProvider>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbInitializer));

        if (dbSettings.MigrateOnStartup)
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("データベースの初期化が完了しました（{Provider}）", dbSettings.Provider);
        }

        await SeedMasterAsync(db, authSettings, clock);
        if (dbSettings.SeedSampleData) await SeedSampleAsync(db, clock);
    }

    /// <summary>マスタの初期値（日種別・祝日・初期システム管理者）</summary>
    public static async Task SeedMasterAsync(ShareMasterDbContext db, AuthSettings auth, TimeProvider clock)
    {
        if (!await db.DayTypes.AnyAsync())
        {
            db.DayTypes.AddRange(
                new DayType { Code = "WORKDAY", Name = "稼働日", Color = "#607D8B", IsHoliday = false, SortOrder = 10, IsSystem = true },
                new DayType { Code = "HALFDAY", Name = "半日稼働", Color = "#F9A825", IsHoliday = false, SortOrder = 20 },
                new DayType { Code = "COMPANY", Name = "会社休日", Color = "#2E7D32", IsHoliday = true, SortOrder = 30 },
                new DayType { Code = "SUMMER", Name = "夏季休暇", Color = "#EF6C00", IsHoliday = true, SortOrder = 40 },
                new DayType { Code = "NEWYEAR", Name = "年末年始", Color = "#00838F", IsHoliday = true, SortOrder = 50 },
                new DayType { Code = "SPECIAL", Name = "特別休日", Color = "#AD1457", IsHoliday = true, SortOrder = 60 });
        }

        if (!await db.PublicHolidays.AnyAsync())
        {
            var year = clock.GetLocalNow().Year;
            for (var y = year - 1; y <= year + 2; y++)
                await PublicHolidaysController.AddGeneratedAsync(db, y, CancellationToken.None);
        }

        if (!await db.SystemAdmins.AnyAsync())
        {
            var admins = auth.BootstrapAdmins.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToLowerInvariant()).Distinct().ToList();
            if (auth.IsDevelopmentMode && admins.Count == 0 && auth.DevUsers.Count > 0)
                admins.Add(auth.DevUsers[0].UserKey.ToLowerInvariant());
            foreach (var key in admins)
            {
                var name = auth.DevUsers.FirstOrDefault(x => string.Equals(x.UserKey, key, StringComparison.OrdinalIgnoreCase))?.DisplayName ?? key;
                db.SystemAdmins.Add(new SystemAdmin { UserKey = key, DisplayName = name, CreatedAt = clock.GetUtcNow(), CreatedBy = "system" });
            }
        }
        await db.SaveChangesAsync();
    }

    /// <summary>開発用サンプルデータ（本社＞滋賀＞システムA など）</summary>
    public static async Task SeedSampleAsync(ShareMasterDbContext db, TimeProvider clock)
    {
        if (await db.Calendars.AnyAsync()) return;
        var now = clock.GetUtcNow();
        Calendar New(string code, string name, Calendar? parent, int sort, string? desc = null) => new()
        {
            Code = code, Name = name, Parent = parent, SortOrder = sort, Description = desc,
            CreatedAt = now, CreatedBy = "system", UpdatedAt = now, UpdatedBy = "system",
        };

        var hq = New("HQ", "本社", null, 10, "全社共通のカレンダー");
        var shiga = New("SHIGA", "滋賀", hq, 20, "滋賀事業所");
        var sysA = New("SYS-A", "システムA", shiga, 30);
        var komaki = New("KOMAKI", "小牧", hq, 40, "小牧事業所");
        var sysB = New("SYS-B", "システムB", komaki, 50);
        var sysC = New("SYS-C", "システムC", hq, 60, "日曜のみ定休・祝日稼働");
        sysC.WeeklyHolidays = "0";
        sysC.UsePublicHolidays = false;

        hq.Managers.Add(new CalendarManager { UserKey = "admin@dev.local", DisplayName = "管理者 太郎" });
        shiga.Managers.Add(new CalendarManager { UserKey = "shiga@dev.local", DisplayName = "滋賀 花子" });
        sysA.Managers.Add(new CalendarManager { UserKey = "shiga@dev.local", DisplayName = "滋賀 花子" });
        komaki.Managers.Add(new CalendarManager { UserKey = "komaki@dev.local", DisplayName = "小牧 次郎" });
        sysB.Managers.Add(new CalendarManager { UserKey = "komaki@dev.local", DisplayName = "小牧 次郎" });
        db.Calendars.AddRange(hq, shiga, sysA, komaki, sysB, sysC);
        await db.SaveChangesAsync();

        var types = await db.DayTypes.ToDictionaryAsync(x => x.Code, x => x.Id);
        var year = clock.GetLocalNow().Year;
        void Day(Calendar c, DateOnly d, string? type, string? name = null, string? note = null) => db.CalendarDays.Add(new CalendarDay
        {
            CalendarId = c.Id, Date = d, DayTypeId = type is null ? null : types[type], Name = name, Note = note,
            CreatedAt = now, CreatedBy = "system", UpdatedAt = now, UpdatedBy = "system",
        });

        for (var y = year - 1; y <= year; y++)
        {
            foreach (var d in new[] { 29, 30, 31 }) Day(hq, new DateOnly(y, 12, d), "NEWYEAR");
            foreach (var d in new[] { 2, 3 }) Day(hq, new DateOnly(y + 1, 1, d), "NEWYEAR");
            foreach (var d in new[] { 13, 14, 15 }) Day(hq, new DateOnly(y, 8, d), "SUMMER");
            Day(shiga, new DateOnly(y, 5, 1), "SPECIAL", "創立記念日");
            Day(shiga, new DateOnly(y, 10, 15), null, null, "工場設備点検（午後停止）");
            Day(komaki, new DateOnly(y, 8, 15), "WORKDAY", null, "繁忙期のため出勤");
            Day(sysA, new DateOnly(y, 11, 20), null, null, "定期リリース日");
            Day(sysB, new DateOnly(y, 6, 30), "HALFDAY", null, "半期末棚卸");
        }
        await db.SaveChangesAsync();
    }
}
