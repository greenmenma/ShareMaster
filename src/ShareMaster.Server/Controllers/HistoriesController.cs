using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareMaster.Server.Contracts;
using ShareMaster.Server.Data;

namespace ShareMaster.Server.Controllers;

public sealed class HistoriesController(ShareMasterDbContext db) : ApiControllerBase
{
    [HttpGet]
    public async Task<PagedResult<HistoryDto>> Get(
        [FromQuery] int? calendarId,
        [FromQuery] string? entityType,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var q = db.ChangeHistories.AsNoTracking();
        if (calendarId is int id) q = q.Where(x => x.CalendarId == id);
        if (!string.IsNullOrWhiteSpace(entityType)) q = q.Where(x => x.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(keyword)) q = q.Where(x => x.Summary.Contains(keyword) || x.ChangedByName.Contains(keyword));

        var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var names = await db.Calendars.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        var items = rows.Select(x => new HistoryDto(
            x.Id, x.CalendarId, x.CalendarId is int cid ? names.GetValueOrDefault(cid) : null,
            x.EntityType, x.EntityKey, x.Action, x.Summary, x.BeforeJson, x.AfterJson,
            x.ChangedBy, x.ChangedByName, x.ChangedAt)).ToList();
        return new PagedResult<HistoryDto>(items, total, page, pageSize);
    }
}
