using Microsoft.EntityFrameworkCore;
using ShareMaster.Server.Auth;
using ShareMaster.Server.Data;
using ShareMaster.Server.Data.Entities;
using ShareMaster.Server.Domain;

namespace ShareMaster.Server.Services;

/// <summary>
/// 親カレンダー更新時に子孫カレンダーの管理者へ通知する。
/// 現在はアプリ内通知のみ。メール・Teams 等は INotificationChannel を追加して拡張する想定。
/// </summary>
public sealed class NotificationService(ShareMasterDbContext db, ICurrentUser user, TimeProvider clock)
{
    /// <summary>子孫カレンダー管理者への通知を追加する（保存は呼び出し元）。通知人数を返す</summary>
    public async Task<int> NotifyDescendantManagersAsync(Calendar calendar, string message, CancellationToken ct)
    {
        var nodes = await db.Calendars.Where(x => x.IsActive)
            .Select(x => new CalendarNode(x.Id, x.Name, x.ParentId, x.WeeklyHolidays, x.UsePublicHolidays))
            .ToListAsync(ct);
        var descendants = new CalendarResolver(nodes, [], [], []).GetDescendants(calendar.Id);
        if (descendants.Count == 0) return 0;

        var targets = await db.CalendarManagers
            .Where(x => descendants.Contains(x.CalendarId) && x.ReceiveNotifications && x.UserKey != user.UserKey)
            .Select(x => new { x.UserKey, x.CalendarId })
            .ToListAsync(ct);

        var now = clock.GetUtcNow();
        var names = nodes.ToDictionary(x => x.Id, x => x.Name);
        foreach (var g in targets.GroupBy(x => x.UserKey))
        {
            var affected = string.Join("、", g.Select(x => names.GetValueOrDefault(x.CalendarId)).Distinct());
            db.Notifications.Add(new Notification
            {
                UserKey = g.Key,
                CalendarId = calendar.Id,
                Title = $"親カレンダー「{calendar.Name}」が更新されました",
                Message = $"{message}（影響を受ける管理カレンダー：{affected}／更新者：{user.DisplayName}）",
                LinkUrl = $"/calendars/{g.First().CalendarId}",
                CreatedAt = now,
            });
        }
        return targets.Select(x => x.UserKey).Distinct().Count();
    }
}
