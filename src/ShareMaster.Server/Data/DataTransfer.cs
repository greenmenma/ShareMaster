using Microsoft.EntityFrameworkCore;
using ShareMaster.Server.Data.Entities;

namespace ShareMaster.Server.Data;

/// <summary>
/// DB 間のデータ移行（例：SQLite → SQL Server）。ID を保持したまま全テーブルを複写する。
/// 移行先はマイグレーション適用済みかつ空であること。
/// </summary>
public static class DataTransfer
{
    public static async Task<IReadOnlyDictionary<string, int>> CopyAsync(ShareMasterDbContext source, ShareMasterDbContext target, CancellationToken ct = default)
    {
        if (await target.Calendars.AnyAsync(ct) || await target.DayTypes.AnyAsync(ct) || await target.ChangeHistories.AnyAsync(ct))
            throw new InvalidOperationException("移行先のデータベースが空ではありません。空のデータベースを指定してください。");

        var counts = new Dictionary<string, int>();
        await using var tx = await target.Database.BeginTransactionAsync(ct);

        counts["DayTypes"] = await CopyAsync(source.DayTypes, target, ct);
        counts["PublicHolidays"] = await CopyAsync(source.PublicHolidays, target, ct);
        counts["Users"] = await CopyAsync(source.Users, target, ct);
        counts["SystemAdmins"] = await CopyAsync(source.SystemAdmins, target, ct);

        // カレンダーは親子の外部キーがあるため階層の浅い順に登録する
        var calendars = await source.Calendars.AsNoTracking().ToListAsync(ct);
        var depth = calendars.ToDictionary(c => c.Id, c => Depth(c, calendars));
        counts["Calendars"] = await InsertAsync(target, calendars.OrderBy(c => depth[c.Id]).ThenBy(c => c.Id).ToList(), ct);

        counts["CalendarManagers"] = await CopyAsync(source.CalendarManagers, target, ct);
        counts["CalendarDays"] = await CopyAsync(source.CalendarDays, target, ct);
        counts["ChangeHistories"] = await CopyAsync(source.ChangeHistories, target, ct);
        counts["Notifications"] = await CopyAsync(source.Notifications, target, ct);

        await tx.CommitAsync(ct);
        return counts;
    }

    private static int Depth(Calendar c, List<Calendar> all)
    {
        var d = 0;
        var seen = new HashSet<int>();
        var cur = c;
        while (cur.ParentId is int p && seen.Add(cur.Id) && all.FirstOrDefault(x => x.Id == p) is { } parent)
        {
            d++;
            cur = parent;
        }
        return d;
    }

    private static async Task<int> CopyAsync<T>(IQueryable<T> rows, ShareMasterDbContext target, CancellationToken ct) where T : class =>
        await InsertAsync(target, await rows.AsNoTracking().ToListAsync(ct), ct);

    private static async Task<int> InsertAsync<T>(ShareMasterDbContext target, List<T> rows, CancellationToken ct) where T : class
    {
        if (rows.Count == 0) return 0;
        var entityType = target.Model.FindEntityType(typeof(T))!;
        var table = entityType.GetTableName()!;
        // SQL Server の IDENTITY 列に元の ID を入れるため一時的に IDENTITY_INSERT を有効にする
        var identity = target.Database.IsSqlServer() && entityType.FindPrimaryKey()!.Properties.Any(p => p.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never);
        // テーブル名は EF のモデル由来（利用者入力ではない）。識別子はパラメーター化できないためエスケープして連結する
        var quoted = "[" + table.Replace("]", "]]") + "]";
#pragma warning disable EF1003 // 上記のとおりエスケープ済みのモデル由来の識別子のみを連結している
        if (identity) await target.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT " + quoted + " ON", ct);
        target.AddRange(rows);
        await target.SaveChangesAsync(ct);
        target.ChangeTracker.Clear();
        if (identity) await target.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT " + quoted + " OFF", ct);
#pragma warning restore EF1003
        return rows.Count;
    }
}

/// <summary>
/// コマンドラインからのデータ移行。
///   dotnet ShareMaster.Server.dll copy-data --from "Data Source=App_Data/sharemaster.db"
/// 移行先は通常の設定（Database:Provider と ConnectionStrings:ShareMasterDb）。
/// </summary>
public static class DataTransferCommand
{
    public const string CommandName = "copy-data";

    public static async Task<int> RunAsync(WebApplication app)
    {
        var logger = app.Logger;
        var from = app.Configuration["from"];
        if (string.IsNullOrWhiteSpace(from))
        {
            logger.LogError("移行元の SQLite 接続文字列を --from で指定してください。例：copy-data --from \"Data Source=App_Data/sharemaster.db\"");
            return 1;
        }

        using var scope = app.Services.CreateScope();
        var target = scope.ServiceProvider.GetRequiredService<ShareMasterDbContext>();
        logger.LogInformation("移行先：{Provider}", target.Database.ProviderName);
        await target.Database.MigrateAsync();

        await using var source = new SqliteShareMasterDbContext(
            new DbContextOptionsBuilder<SqliteShareMasterDbContext>().UseSqlite(from).Options);
        await source.Database.MigrateAsync();

        // 再試行戦略はユーザー開始トランザクションと併用できないため、実行戦略の中で行う
        var strategy = target.Database.CreateExecutionStrategy();
        var counts = await strategy.ExecuteAsync(() => DataTransfer.CopyAsync(source, target));
        foreach (var (table, count) in counts) logger.LogInformation("{Table}: {Count} 件", table, count);
        logger.LogInformation("データ移行が完了しました。");
        return 0;
    }
}
