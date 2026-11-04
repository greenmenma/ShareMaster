using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShareMaster.Server.Auth;
using ShareMaster.Server.Contracts;
using ShareMaster.Server.Data;
using ShareMaster.Server.Data.Entities;
using ShareMaster.Server.Services;

namespace ShareMaster.Server.Controllers;

[Route("api/system-admins")]
[RequireSystemAdmin]
public sealed class SystemAdminsController(ShareMasterDbContext db, ICurrentUser user, HistoryWriter history, TimeProvider clock) : ApiControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<SystemAdminDto>> GetAll(CancellationToken ct) =>
        await db.SystemAdmins.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new SystemAdminDto(x.Id, x.UserKey, x.DisplayName, x.CreatedAt, x.CreatedBy)).ToListAsync(ct);

    [HttpPost]
    public async Task<SystemAdminDto> Add(SystemAdminSaveRequest req, CancellationToken ct)
    {
        var key = req.UserKey.Trim().ToLowerInvariant();
        if (await db.SystemAdmins.AnyAsync(x => x.UserKey == key, ct)) throw ServiceException.Conflict("既にシステム管理者です。");
        var known = await db.Users.FindAsync([key], ct);
        var e = new SystemAdmin
        {
            UserKey = key,
            DisplayName = string.IsNullOrWhiteSpace(req.DisplayName) ? known?.DisplayName ?? key : req.DisplayName.Trim(),
            CreatedAt = clock.GetUtcNow(),
            CreatedBy = user.UserKey,
        };
        db.SystemAdmins.Add(e);
        history.Add(nameof(SystemAdmin), key, "Create", $"システム管理者に「{e.DisplayName}」を追加", null, new { e.UserKey, e.DisplayName });
        await db.SaveChangesAsync(ct);
        return new SystemAdminDto(e.Id, e.UserKey, e.DisplayName, e.CreatedAt, e.CreatedBy);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id, CancellationToken ct)
    {
        var e = await db.SystemAdmins.FindAsync([id], ct) ?? throw ServiceException.NotFound();
        if (await db.SystemAdmins.CountAsync(ct) <= 1) throw ServiceException.BadRequest("システム管理者を 0 人にすることはできません。");
        db.SystemAdmins.Remove(e);
        history.Add(nameof(SystemAdmin), e.UserKey, "Delete", $"システム管理者から「{e.DisplayName}」を削除", new { e.UserKey, e.DisplayName }, null);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
