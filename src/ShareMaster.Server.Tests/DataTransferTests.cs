using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ShareMaster.Server.Auth;
using ShareMaster.Server.Data;

namespace ShareMaster.Server.Tests;

public sealed class DataTransferTests : IAsyncLifetime
{
    private readonly SqliteConnection _srcConn = new("DataSource=:memory:");
    private readonly SqliteConnection _dstConn = new("DataSource=:memory:");
    private SqliteShareMasterDbContext _src = null!;
    private SqliteShareMasterDbContext _dst = null!;

    private static SqliteShareMasterDbContext Create(SqliteConnection c) =>
        new(new DbContextOptionsBuilder<SqliteShareMasterDbContext>().UseSqlite(c).Options);

    public async Task InitializeAsync()
    {
        await _srcConn.OpenAsync();
        await _dstConn.OpenAsync();
        _src = Create(_srcConn);
        _dst = Create(_dstConn);
        await _src.Database.MigrateAsync();
        await _dst.Database.MigrateAsync();
        var auth = new AuthSettings { Mode = "Development", DevUsers = [new DevUser { UserKey = "admin@dev.local", DisplayName = "管理者" }] };
        await DbInitializer.SeedMasterAsync(_src, auth, TimeProvider.System);
        await DbInitializer.SeedSampleAsync(_src, TimeProvider.System);
    }

    public async Task DisposeAsync()
    {
        await _src.DisposeAsync();
        await _dst.DisposeAsync();
        await _srcConn.DisposeAsync();
        await _dstConn.DisposeAsync();
    }

    [Fact]
    public async Task 全テーブルがIDと親子関係を保ったまま複写される()
    {
        var counts = await DataTransfer.CopyAsync(_src, _dst);

        Assert.Equal(await _src.Calendars.CountAsync(), counts["Calendars"]);
        Assert.Equal(await _src.CalendarDays.CountAsync(), await _dst.CalendarDays.CountAsync());
        Assert.Equal(await _src.PublicHolidays.CountAsync(), await _dst.PublicHolidays.CountAsync());

        var srcTree = await _src.Calendars.OrderBy(x => x.Id).Select(x => new { x.Id, x.Code, x.ParentId }).ToListAsync();
        var dstTree = await _dst.Calendars.OrderBy(x => x.Id).Select(x => new { x.Id, x.Code, x.ParentId }).ToListAsync();
        Assert.Equal(srcTree, dstTree);
    }

    [Fact]
    public async Task 移行先が空でなければエラー()
    {
        await DataTransfer.CopyAsync(_src, _dst);
        _dst.ChangeTracker.Clear();
        await Assert.ThrowsAsync<InvalidOperationException>(() => DataTransfer.CopyAsync(_src, _dst));
    }
}
