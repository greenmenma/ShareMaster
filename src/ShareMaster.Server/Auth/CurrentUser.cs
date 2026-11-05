using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ShareMaster.Server.Data;

namespace ShareMaster.Server.Auth;

/// <summary>ログイン中のユーザー情報と権限判定</summary>
public interface ICurrentUser
{
    string UserKey { get; }
    string DisplayName { get; }
    string? Email { get; }
    Task<bool> IsSystemAdminAsync(CancellationToken ct = default);
    /// <summary>カレンダーの編集可否（システム管理者、または対象もしくは祖先カレンダーの管理者）</summary>
    Task<bool> CanEditCalendarAsync(int calendarId, CancellationToken ct = default);
}

public sealed class CurrentUser(IHttpContextAccessor accessor, ShareMasterDbContext db) : ICurrentUser
{
    private bool? _isAdmin;
    private HashSet<int>? _managedCalendarIds;

    private ClaimsPrincipal Principal => accessor.HttpContext?.User ?? new ClaimsPrincipal();

    public string UserKey =>
        (Principal.FindFirstValue("preferred_username")
         ?? Principal.FindFirstValue(ClaimTypes.Upn)
         ?? Principal.FindFirstValue(ClaimTypes.Email)
         ?? Principal.FindFirstValue("oid")
         // Windows 認証は名前（DOMAIN\user 形式）のみ持つ
         ?? Principal.Identity?.Name
         ?? "anonymous").ToLowerInvariant();

    public string DisplayName => Principal.FindFirstValue("name") ?? Principal.Identity?.Name ?? UserKey;

    public string? Email => Principal.FindFirstValue(ClaimTypes.Email) ?? Principal.FindFirstValue("email") ?? Principal.FindFirstValue("preferred_username");

    public async Task<bool> IsSystemAdminAsync(CancellationToken ct = default)
    {
        _isAdmin ??= await db.SystemAdmins.AnyAsync(x => x.UserKey == UserKey, ct);
        return _isAdmin.Value;
    }

    public async Task<bool> CanEditCalendarAsync(int calendarId, CancellationToken ct = default)
    {
        if (await IsSystemAdminAsync(ct)) return true;
        _managedCalendarIds ??= (await db.CalendarManagers.Where(x => x.UserKey == UserKey).Select(x => x.CalendarId).ToListAsync(ct)).ToHashSet();
        if (_managedCalendarIds.Count == 0) return false;

        // 祖先を辿って管理対象が含まれるか確認する
        var parents = await db.Calendars.Select(x => new { x.Id, x.ParentId }).ToDictionaryAsync(x => x.Id, x => x.ParentId, ct);
        var visited = new HashSet<int>();
        int? current = calendarId;
        while (current is int id && visited.Add(id))
        {
            if (_managedCalendarIds.Contains(id)) return true;
            current = parents.GetValueOrDefault(id);
        }
        return false;
    }
}
