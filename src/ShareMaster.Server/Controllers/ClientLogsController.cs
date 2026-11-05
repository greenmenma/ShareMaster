using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareMaster.Server.Contracts;

namespace ShareMaster.Server.Controllers;

/// <summary>
/// 画面（ブラウザ）で発生したエラーをサーバーのログへ記録する。
/// 画面が表示できない状況（認証前・JS の読み込み失敗）でも送れるよう未認証で受け付ける。
/// </summary>
[Route("api/client-logs")]
public sealed class ClientLogsController(ILogger<ClientLogsController> logger) : ApiControllerBase
{
    private const int MaxLength = 4000;

    [AllowAnonymous]
    [HttpPost]
    [RequestSizeLimit(64 * 1024)]
    public IActionResult Post([FromBody] ClientLogDto log)
    {
        var level = string.Equals(log.Level, "warning", StringComparison.OrdinalIgnoreCase) ? LogLevel.Warning : LogLevel.Error;
        var user = User.Identity?.IsAuthenticated == true ? User.Identity.Name : "(未認証)";
        logger.Log(level, "画面エラー：{Message}\n  URL：{Url}\n  利用者：{User}、接続元：{RemoteIp}\n  ブラウザ：{UserAgent}\n  詳細：{Detail}",
            Truncate(log.Message), Truncate(log.Url), user, HttpContext.Connection.RemoteIpAddress,
            Truncate(Request.Headers.UserAgent.ToString()), Truncate(log.Detail));
        return NoContent();
    }

    private static string Truncate(string? value) =>
        string.IsNullOrEmpty(value) ? "" : value.Length <= MaxLength ? value : value[..MaxLength] + "…";
}
