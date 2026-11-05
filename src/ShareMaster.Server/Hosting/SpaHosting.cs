using System.Net;
using System.Security.Claims;

namespace ShareMaster.Server.Hosting;

/// <summary>
/// React（SPA）の配信。
/// IIS のサブフォルダ（/ShareMaster/ 等）に配置しても動くよう、index.html に実行時の配置パスを
/// &lt;base href&gt; として埋め込んで返す（ビルド時に配置先を指定する必要はない）。
/// </summary>
public static class SpaHosting
{
    private const string IndexFile = "index.html";

    public static IEndpointConventionBuilder MapSpaFallback(this WebApplication app) =>
        app.MapFallback(async context =>
        {
            // 存在しない API は index.html ではなく 404 を返す（画面側でエラーとして扱えるように）
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var file = app.Environment.WebRootFileProvider.GetFileInfo(IndexFile);
            if (!file.Exists)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.WriteAsync("wwwroot/index.html がありません。クライアントを含めて発行されているか確認してください。");
                return;
            }

            string html;
            await using (var stream = file.CreateReadStream())
            using (var reader = new StreamReader(stream))
                html = await reader.ReadToEndAsync(context.RequestAborted);

            context.Response.ContentType = "text/html; charset=utf-8";
            // 再発行後に古い index.html（存在しない JS を参照）がキャッシュされて真っ白になるのを防ぐ
            context.Response.Headers.CacheControl = "no-cache, no-store";
            await context.Response.WriteAsync(InjectBaseHref(html, context.Request.PathBase), context.RequestAborted);
        });

    /// <summary>&lt;head&gt; の直後に &lt;base href="{配置パス}/"&gt; を挿入する</summary>
    public static string InjectBaseHref(string html, PathString pathBase)
    {
        var baseHref = WebUtility.HtmlEncode($"{pathBase.ToUriComponent().TrimEnd('/')}/");
        var index = html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
        if (index < 0) return html;
        index += "<head>".Length;
        return html.Insert(index, $"\n    <base href=\"{baseHref}\" />");
    }
}

/// <summary>
/// エラー応答（4xx/5xx）をログに残す。画面が真っ白になる場合（JS/CSS の 404 等）の原因調査用。
/// </summary>
public sealed class RequestErrorLoggingMiddleware(RequestDelegate next, ILogger<RequestErrorLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        var status = context.Response.StatusCode;
        if (status < 400) return;

        var user = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirstValue("preferred_username") ?? context.User.Identity.Name
            : "(未認証)";
        // 401 は Windows 認証のネゴシエーションでも通常発生するため情報レベルにする
        var level = status >= 500 ? LogLevel.Error : status == StatusCodes.Status401Unauthorized ? LogLevel.Information : LogLevel.Warning;
        logger.Log(level, "HTTP {Method} {PathBase}{Path}{Query} → {Status}（利用者：{User}、接続元：{RemoteIp}）",
            context.Request.Method, context.Request.PathBase, context.Request.Path, context.Request.QueryString,
            status, user, context.Connection.RemoteIpAddress);
    }
}
