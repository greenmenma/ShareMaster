using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareMaster.Server.Auth;
using ShareMaster.Server.Contracts;
using ShareMaster.Server.Data;

namespace ShareMaster.Server.Controllers;

public sealed class NotificationsController(ShareMasterDbContext db, ICurrentUser user) : ApiControllerBase
{
    [HttpGet]
    public async Task<NotificationSummaryDto> Get([FromQuery] int take = 30, CancellationToken ct = default)
    {
        var q = db.Notifications.AsNoTracking().Where(x => x.UserKey == user.UserKey);
        var unread = await q.CountAsync(x => !x.IsRead, ct);
        var items = await q.OrderByDescending(x => x.Id).Take(Math.Clamp(take, 1, 100))
            .Select(x => new NotificationDto(x.Id, x.CalendarId, x.Title, x.Message, x.LinkUrl, x.IsRead, x.CreatedAt))
            .ToListAsync(ct);
        return new NotificationSummaryDto(unread, items);
    }

    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id, CancellationToken ct)
    {
        await db.Notifications.Where(x => x.Id == id && x.UserKey == user.UserKey)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true), ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await db.Notifications.Where(x => x.UserKey == user.UserKey && !x.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true), ct);
        return NoContent();
    }
}
