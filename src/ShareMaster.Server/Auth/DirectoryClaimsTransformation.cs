using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace ShareMaster.Server.Auth;

/// <summary>
/// Windows 認証の利用者に、AD の表示名（displayName）とメールアドレスを付与する。
/// Windows 認証のトークンには "DOMAIN\user" しか含まれないため、AD から取得して "name" / "email" クレームにする。
/// AD への問い合わせを減らすため、結果は一定時間キャッシュする。
/// </summary>
public sealed class DirectoryClaimsTransformation(IUserDirectory directory, IMemoryCache cache, ILogger<DirectoryClaimsTransformation> logger)
    : IClaimsTransformation
{
    private const string NameClaim = "name";
    private const string EmailClaim = "email";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var name = principal.Identity?.Name;
        // 認証済みで、まだ付与していない場合のみ（1 リクエストで複数回呼ばれることがある）
        if (principal.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(name) || principal.HasClaim(c => c.Type == NameClaim))
            return principal;

        var key = name.ToLowerInvariant();
        var user = await cache.GetOrCreateAsync($"directory-user:{key}", async entry =>
        {
            try
            {
                var found = await directory.FindAsync(key);
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return found;
            }
            catch (Exception ex)
            {
                // 取得できなくてもサインインは継続する（表示名はユーザー名になる）
                logger.LogWarning(ex, "AD から利用者 {User} の表示名を取得できませんでした", key);
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return null;
            }
        });
        if (user is null) return principal;

        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(NameClaim, user.DisplayName));
        if (!string.IsNullOrEmpty(user.Email)) identity.AddClaim(new Claim(EmailClaim, user.Email));
        principal.AddIdentity(identity);
        return principal;
    }
}
