using System.Net;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;

namespace ShareMaster.Server.Hosting;

/// <summary>
/// HTTP → HTTPS、ホスト名のみ → FQDN（ドメイン付与）へリダイレクトする。
/// 両方に該当する場合も 1 回のリダイレクトで正規 URL へ移動させる。
/// </summary>
public sealed class CanonicalUrlMiddleware(RequestDelegate next, IOptions<CanonicalUrlSettings> options)
{
    public Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var location = BuildRedirectUrl(
            options.Value, request.IsHttps, request.Host, request.PathBase, request.Path, request.QueryString);
        if (location is null) return next(context);

        // 一時リダイレクト（307）：設定誤りがブラウザにキャッシュされないようにし、メソッドも維持する
        context.Response.StatusCode = StatusCodes.Status307TemporaryRedirect;
        context.Response.Headers.Location = location;
        return Task.CompletedTask;
    }

    /// <summary>リダイレクト先の URL を返す。リダイレクト不要なら null。</summary>
    public static string? BuildRedirectUrl(
        CanonicalUrlSettings settings, bool isHttps, HostString host, PathString pathBase, PathString path, QueryString query)
    {
        if (!host.HasValue) return null;

        var toHttps = settings.RequireHttps && !isHttps;
        var domain = NormalizeDomain(settings.Domain);
        var appendDomain = domain is not null && IsBareHostName(host.Host);
        if (!toHttps && !appendDomain) return null;

        var hostName = appendDomain ? $"{host.Host}.{domain}" : host.Host;
        HostString newHost;
        if (toHttps)
        {
            // HTTP 用のポートは引き継がず、HTTPS ポートを使う（443 は省略）
            var port = settings.HttpsPort ?? 443;
            newHost = port == 443 ? new HostString(hostName) : new HostString(hostName, port);
        }
        else
        {
            newHost = host.Port is { } p ? new HostString(hostName, p) : new HostString(hostName);
        }

        return UriHelper.BuildAbsolute(toHttps || isHttps ? "https" : "http", newHost, pathBase, path, query);
    }

    /// <summary>"*.hoge.co.jp" / ".hoge.co.jp" / "hoge.co.jp" を "hoge.co.jp" に揃える。空なら null。</summary>
    public static string? NormalizeDomain(string? domain)
    {
        var d = (domain ?? "").Trim();
        if (d.StartsWith("*.", StringComparison.Ordinal)) d = d[2..];
        d = d.Trim('.');
        return d.Length == 0 ? null : d;
    }

    /// <summary>ドットを含まないホスト名か（localhost・IP アドレスは除く）</summary>
    public static bool IsBareHostName(string host)
    {
        if (string.IsNullOrEmpty(host) || host.Contains('.')) return false;
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)) return false;
        // IPv6（[::1] 等）は対象外
        return !IPAddress.TryParse(host.Trim('[', ']'), out _);
    }
}
