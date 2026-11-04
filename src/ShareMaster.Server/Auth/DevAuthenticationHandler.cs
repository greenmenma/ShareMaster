using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ShareMaster.Server.Auth;

/// <summary>
/// ローカル開発用の擬似認証。リクエストヘッダー "X-Dev-User" で指定された開発ユーザーとしてログインする。
/// 本番環境（Production）では登録されない。
/// </summary>
public sealed class DevAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<AuthSettings> settings)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Dev";
    public const string HeaderName = "X-Dev-User";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var users = settings.Value.DevUsers;
        if (users.Count == 0) return Task.FromResult(AuthenticateResult.NoResult());

        var requested = Request.Headers[HeaderName].ToString();
        var user = users.FirstOrDefault(x => string.Equals(x.UserKey, requested, StringComparison.OrdinalIgnoreCase)) ?? users[0];

        var identity = new ClaimsIdentity(
        [
            new Claim("preferred_username", user.UserKey),
            new Claim("name", user.DisplayName),
            new Claim(ClaimTypes.Email, user.UserKey),
        ], SchemeName, "name", ClaimTypes.Role);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
