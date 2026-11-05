using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ShareMaster.Server.Auth;
using ShareMaster.Server.Data;
using ShareMaster.Server.Services;

namespace ShareMaster.Server.Tests;

public sealed class DataMaintenanceServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _conn = new("DataSource=:memory:");
    private ShareMasterDbContext _db = null!;
    private DataMaintenanceService _service = null!;
    // HttpContext は AsyncLocal で保持されるため、テスト本体へ引き継がれるようコンストラクター（フィールド）で作る
    private readonly HttpContextAccessor _http = new() { HttpContext = new DefaultHttpContext() };
    private readonly AuthSettings _auth = new() { Mode = "Development", DevUsers = [new DevUser { UserKey = "admin@dev.local", DisplayName = "管理者" }] };

    public async Task InitializeAsync()
    {
        await _conn.OpenAsync();
        _db = new SqliteShareMasterDbContext(new DbContextOptionsBuilder<SqliteShareMasterDbContext>().UseSqlite(_conn).Options);
        await _db.Database.MigrateAsync();
        await DbInitializer.SeedMasterAsync(_db, _auth, TimeProvider.System);
        await DbInitializer.SeedSampleAsync(_db, TimeProvider.System);

        _http.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("preferred_username", "corp\\taro"), new Claim("name", "山田 太郎")], "Test"));
        var user = new CurrentUser(_http, _db);
        _service = new DataMaintenanceService(_db, user, new HistoryWriter(_db, user, TimeProvider.System), Options.Create(_auth),
            TimeProvider.System, NullLogger<DataMaintenanceService>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _conn.DisposeAsync();
    }

    [Fact]
    public async Task データクリアでシステム管理者以外を削除しマスタを初期値に戻す()
    {
        _db.DayTypes.Add(new() { Code = "EXTRA", Name = "追加", Color = "#000000" });
        await _db.SaveChangesAsync();

        await _service.ClearAllAsync(default);
        _db.ChangeTracker.Clear();

        Assert.Equal(0, await _db.Calendars.CountAsync());
        Assert.Equal(0, await _db.CalendarDays.CountAsync());
        Assert.Equal(0, await _db.CalendarManagers.CountAsync());
        Assert.False(await _db.DayTypes.AnyAsync(x => x.Code == "EXTRA"));
        Assert.True(await _db.DayTypes.AnyAsync(x => x.Code == "WORKDAY"));
        Assert.True(await _db.PublicHolidays.AnyAsync());
        Assert.True(await _db.SystemAdmins.AnyAsync());
        // クリアの操作だけが履歴に残る
        Assert.Equal("Clear", (await _db.ChangeHistories.SingleAsync()).Action);
    }

    [Fact]
    public async Task サンプルデータは操作した本人を本社の管理者にする()
    {
        await _service.ClearAllAsync(default);
        await _service.InsertSampleAsync(default);
        _db.ChangeTracker.Clear();

        Assert.Equal(6, await _db.Calendars.CountAsync());
        var manager = await _db.CalendarManagers.Include(x => x.Calendar).SingleAsync();
        Assert.Equal("HQ", manager.Calendar!.Code);
        Assert.Equal("corp\\taro", manager.UserKey);
        Assert.Equal("山田 太郎", manager.DisplayName);
    }

    [Fact]
    public async Task カレンダーがあるとサンプルデータは挿入できない()
    {
        var ex = await Assert.ThrowsAsync<ServiceException>(() => _service.InsertSampleAsync(default));
        Assert.Contains("データクリア", ex.Message);
    }

    [Theory]
    [InlineData("taro", "taro")]
    [InlineData("a*b", @"a\2ab")]
    [InlineData("(x)", @"\28x\29")]
    [InlineData(@"corp\taro", @"corp\5ctaro")]
    public void LDAPフィルターの特殊文字をエスケープする(string input, string expected)
        => Assert.Equal(expected, ActiveDirectoryUserDirectory.Escape(input));
}
