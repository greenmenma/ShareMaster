using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShareMaster.Server.Auth;
using ShareMaster.Server.Contracts;
using ShareMaster.Server.Data;
using ShareMaster.Server.Data.Entities;
using ShareMaster.Server.Services;

namespace ShareMaster.Server.Controllers;

[Route("api")]
public sealed class AuthController(
    IOptions<AuthSettings> settings, ICurrentUser user, ShareMasterDbContext db, TimeProvider clock,
    IUserDirectory directory, HistoryWriter history, ILogger<AuthController> logger) : ApiControllerBase
{
    /// <summary>SPA が起動時に取得する認証設定（未認証で取得可能）</summary>
    [AllowAnonymous]
    [HttpGet("auth-config")]
    public AuthConfigDto GetAuthConfig()
    {
        var s = settings.Value;
        return new AuthConfigDto(
            s.NormalizedMode,
            s.Spa.ClientId, s.Spa.Authority, s.Spa.Scopes,
            s.IsDevelopmentMode ? s.DevUsers.Select(x => new DevUserDto(x.UserKey, x.DisplayName)).ToList() : []);
    }

    /// <summary>
    /// ログインユーザー情報。利用者マスタへ自動登録する。
    /// システム管理者が 1 人もいない場合は、最初にログインした利用者をシステム管理者として登録する。
    /// </summary>
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

        if (!await db.SystemAdmins.AnyAsync(ct))
        {
            var admin = new SystemAdmin { UserKey = user.UserKey, DisplayName = user.DisplayName, CreatedAt = clock.GetUtcNow(), CreatedBy = "system" };
            db.SystemAdmins.Add(admin);
            history.Add(nameof(SystemAdmin), admin.UserKey, "Create", $"最初にログインした「{admin.DisplayName}」をシステム管理者に登録", null, new { admin.UserKey, admin.DisplayName });
            logger.LogWarning("システム管理者が未登録のため、最初にログインした {User}（{Name}）をシステム管理者に登録しました", admin.UserKey, admin.DisplayName);
        }
        await db.SaveChangesAsync(ct);
        return new MeDto(user.UserKey, user.DisplayName, user.Email, await user.IsSystemAdminAsync(ct));
    }

    /// <summary>
    /// 利用者検索（管理者設定の候補）。Windows 認証では Active Directory、Entra ID では Graph API から検索する
    /// </summary>
    [HttpGet("users")]
    public async Task<IReadOnlyList<UserDto>> SearchUsers([FromQuery] string? search, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(search)) return [];
        var users = await directory.SearchAsync(search, 30, ct);
        return users.Select(x => new UserDto(x.UserKey, x.DisplayName, x.Email, x.Department)).ToList();
    }
}
