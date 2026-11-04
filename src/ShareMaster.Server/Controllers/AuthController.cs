using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShareMaster.Server.Auth;
using ShareMaster.Server.Contracts;
using ShareMaster.Server.Data;
using ShareMaster.Server.Data.Entities;

namespace ShareMaster.Server.Controllers;

[Route("api")]
public sealed class AuthController(IOptions<AuthSettings> settings, ICurrentUser user, ShareMasterDbContext db, TimeProvider clock) : ApiControllerBase
{
    /// <summary>SPA が起動時に取得する認証設定（未認証で取得可能）</summary>
    [AllowAnonymous]
    [HttpGet("auth-config")]
    public AuthConfigDto GetAuthConfig()
    {
        var s = settings.Value;
        return new AuthConfigDto(
            s.IsDevelopmentMode ? AuthSettings.ModeDevelopment : AuthSettings.ModeEntraId,
            s.Spa.ClientId, s.Spa.Authority, s.Spa.Scopes,
            s.IsDevelopmentMode ? s.DevUsers.Select(x => new DevUserDto(x.UserKey, x.DisplayName)).ToList() : []);
    }

    /// <summary>ログインユーザー情報。利用者マスタへ自動登録する</summary>
    [HttpGet("me")]
    public async Task<MeDto> GetMe(CancellationToken ct)
    {
        var row = await db.Users.FindAsync([user.UserKey], ct);
        if (row is null)
        {
            row = new AppUser { UserKey = user.UserKey };
            db.Users.Add(row);
        }
        row.DisplayName = user.DisplayName;
        row.Email = user.Email;
        row.LastAccessedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return new MeDto(user.UserKey, user.DisplayName, user.Email, await user.IsSystemAdminAsync(ct));
    }

    /// <summary>利用者検索（管理者設定の候補）</summary>
    [HttpGet("users")]
    public async Task<IReadOnlyList<UserDto>> SearchUsers([FromQuery] string? search, CancellationToken ct)
    {
        var q = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.UserKey.Contains(search) || x.DisplayName.Contains(search));
        return await q.OrderBy(x => x.DisplayName).Take(50).Select(x => new UserDto(x.UserKey, x.DisplayName, x.Email)).ToListAsync(ct);
    }
}
