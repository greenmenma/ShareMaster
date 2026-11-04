using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ShareMaster.Server.Auth;
using ShareMaster.Server.Contracts;
using ShareMaster.Server.Data;
using ShareMaster.Server.Services;

namespace ShareMaster.Server.Tests;

public sealed class CalendarServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _conn = new("DataSource=:memory:");
    private ShareMasterDbContext _db = null!;
    private readonly HttpContextAccessor _http = new() { HttpContext = new DefaultHttpContext() };
    private CalendarService _service = null!;

    public async Task InitializeAsync()
    {
        await _conn.OpenAsync();
        _db = new SqliteShareMasterDbContext(new DbContextOptionsBuilder<SqliteShareMasterDbContext>().UseSqlite(_conn).Options);
        // 本番と同じマイグレーションでスキーマを作成する（マイグレーションの検証も兼ねる）
        await _db.Database.MigrateAsync();
        var auth = new AuthSettings { Mode = "Development", DevUsers = [new DevUser { UserKey = "admin@dev.local", DisplayName = "管理者" }] };
        await DbInitializer.SeedMasterAsync(_db, auth, TimeProvider.System);
        await DbInitializer.SeedSampleAsync(_db, TimeProvider.System);
        LoginAs("admin@dev.local");
    }

    /// <summary>指定ユーザーでログインした状態のサービスを作り直す（CurrentUser は権限をキャッシュするため）</summary>
    private void LoginAs(string userKey)
    {
        _http.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("preferred_username", userKey), new Claim("name", userKey)], "Test"));
        var user = new CurrentUser(_http, _db);
        var history = new HistoryWriter(_db, user, TimeProvider.System);
        _service = new CalendarService(_db, user, history, new NotificationService(_db, user, TimeProvider.System), TimeProvider.System);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _conn.DisposeAsync();
    }

    private async Task<int> IdOf(string code) => await _db.Calendars.Where(x => x.Code == code).Select(x => x.Id).SingleAsync();

    [Fact]
    public async Task 親カレンダーの更新で子孫の管理者に通知され履歴が残る()
    {
        var hq = await IdOf("HQ");
        var companyType = await _db.DayTypes.Where(x => x.Code == "COMPANY").Select(x => x.Id).SingleAsync();

        var result = await _service.UpdateDaysAsync(hq, new DayUpdateRequest { From = new DateOnly(2030, 4, 1), DayTypeId = companyType, Name = "臨時休業" }, default);

        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(2, result.NotifiedUserCount); // 滋賀・小牧の管理者（更新者本人は除外）
        Assert.Equal(2, await _db.Notifications.CountAsync());
        Assert.Contains(await _db.ChangeHistories.Select(x => x.Summary).ToListAsync(), s => s.Contains("2030/04/01"));

        var sysA = await IdOf("SYS-A");
        var days = await _service.GetDaysAsync([sysA], new DateOnly(2030, 4, 1), new DateOnly(2030, 4, 1), default);
        Assert.True(days.Single().Days.Single().IsHoliday);
        Assert.Equal("臨時休業", days.Single().Days.Single().Name);
    }

    [Fact]
    public async Task 期間と曜日を指定して一括設定でき継承に戻せる()
    {
        var komaki = await IdOf("KOMAKI");
        var workday = await _db.DayTypes.Where(x => x.Code == "WORKDAY").Select(x => x.Id).SingleAsync();
        var req = new DayUpdateRequest { From = new DateOnly(2030, 6, 1), To = new DateOnly(2030, 6, 30), DaysOfWeek = [6], DayTypeId = workday };

        var result = await _service.UpdateDaysAsync(komaki, req, default);
        Assert.Equal(5, result.UpdatedCount); // 2030 年 6 月の土曜日は 5 日

        var back = await _service.UpdateDaysAsync(komaki, new DayUpdateRequest { From = req.From, To = req.To, Mode = DayUpdateMode.Inherit }, default);
        Assert.Equal(5, back.UpdatedCount);
        Assert.False(await _db.CalendarDays.AnyAsync(x => x.CalendarId == komaki && x.Date.Year == 2030));
    }

    [Fact]
    public async Task 管理者以外は他カレンダーを編集できず新規追加もできない()
    {
        LoginAs("shiga@dev.local");
        var komaki = await IdOf("KOMAKI");
        var sysA = await IdOf("SYS-A");
        await Assert.ThrowsAsync<ServiceException>(() =>
            _service.UpdateDaysAsync(komaki, new DayUpdateRequest { From = new DateOnly(2030, 1, 6), Note = "x" }, default));
        await Assert.ThrowsAsync<ServiceException>(() =>
            _service.CreateAsync(new CalendarSaveRequest { Code = "NEW", Name = "新規" }, default));

        // 自分が管理する滋賀の子孫（システムA）は編集できる
        var ok = await _service.UpdateDaysAsync(sysA, new DayUpdateRequest { From = new DateOnly(2030, 1, 7), Note = "棚卸" }, default);
        Assert.Equal(1, ok.UpdatedCount);
    }

    [Fact]
    public async Task 子孫を親に指定すると循環になるためエラー()
    {
        var hq = await IdOf("HQ");
        var shiga = await IdOf("SHIGA");
        var ex = await Assert.ThrowsAsync<ServiceException>(() =>
            _service.UpdateAsync(hq, new CalendarSaveRequest { Code = "HQ", Name = "本社", ParentId = shiga }, default));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task 子がいるカレンダーは削除できない()
    {
        var ex = await Assert.ThrowsAsync<ServiceException>(() => _service.DeleteAsync(_db.Calendars.Single(x => x.Code == "SHIGA").Id, default));
        Assert.Equal(409, ex.StatusCode);
        await _service.DeleteAsync(await IdOf("SYS-C"), default);
        Assert.DoesNotContain(await _service.GetAllAsync(default), x => x.Name == "システムC");
    }
}
