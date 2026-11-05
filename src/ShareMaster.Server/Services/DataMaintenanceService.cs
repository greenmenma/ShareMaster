using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShareMaster.Server.Auth;
using ShareMaster.Server.Contracts;
using ShareMaster.Server.Data;

namespace ShareMaster.Server.Services;

/// <summary>システム管理者向けのデータ管理（データクリア・サンプルデータ挿入）</summary>
public sealed class DataMaintenanceService(
    ShareMasterDbContext db, ICurrentUser user, HistoryWriter history, IOptions<AuthSettings> auth, TimeProvider clock,
    ILogger<DataMaintenanceService> logger)
{
    private const string EntityType = "Data";

    /// <summary>
    /// システム管理者・利用者以外のすべてのデータ（カレンダー・日別設定・カレンダー管理者・日種別・祝日・通知・変更履歴）を削除し、
    /// 日種別・祝日の初期値を再登録する。クリアした操作だけは変更履歴に残す。
    /// </summary>
    public async Task<DataMaintenanceResultDto> ClearAllAsync(CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var calendars = await db.Calendars.CountAsync(ct);
        var days = await db.CalendarDays.CountAsync(ct);
        var histories = await db.ChangeHistories.CountAsync(ct);

        // 外部キーの依存順に削除する（カレンダーは親子参照を外してから削除）
        await db.Notifications.ExecuteDeleteAsync(ct);
        await db.ChangeHistories.ExecuteDeleteAsync(ct);
        await db.CalendarDays.ExecuteDeleteAsync(ct);
        await db.CalendarManagers.ExecuteDeleteAsync(ct);
        await db.Calendars.ExecuteUpdateAsync(x => x.SetProperty(c => c.ParentId, (int?)null), ct);
        await db.Calendars.ExecuteDeleteAsync(ct);
        await db.DayTypes.ExecuteDeleteAsync(ct);
        await db.PublicHolidays.ExecuteDeleteAsync(ct);

        // 日種別・祝日の初期値を再登録（システム管理者は残っているため追加されない）
        await DbInitializer.SeedMasterAsync(db, auth.Value, clock);

        var message = $"データをクリアしました（カレンダー {calendars} 件、日別設定 {days} 件、変更履歴 {histories} 件ほか）。日種別・祝日は初期値に戻しました。";
        history.Add(EntityType, "all", "Clear", message, new { calendars, days, histories }, null);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        logger.LogWarning("{User} がデータクリアを実行しました：{Message}", user.UserKey, message);
        return new DataMaintenanceResultDto(message);
    }

    /// <summary>サンプルデータを挿入する。本社カレンダーの管理者は操作した利用者にする</summary>
    public async Task<DataMaintenanceResultDto> InsertSampleAsync(CancellationToken ct)
    {
        if (await db.Calendars.AnyAsync(ct))
            throw ServiceException.Conflict("カレンダーが登録されているため、サンプルデータを挿入できません。先にデータクリアを実行してください。");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await DbInitializer.SeedSampleAsync(db, clock, (user.UserKey, user.DisplayName));
        var count = await db.Calendars.CountAsync(ct);
        var message = $"サンプルデータを挿入しました（カレンダー {count} 件。本社カレンダーの管理者：{user.DisplayName}）。";
        history.Add(EntityType, "sample", "Create", message, null, new { calendars = count });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        logger.LogInformation("{User} がサンプルデータを挿入しました", user.UserKey);
        return new DataMaintenanceResultDto(message);
    }
}
