namespace ShareMaster.Server.Services;

/// <summary>業務エラー（コントローラーで ProblemDetails に変換する）</summary>
public sealed class ServiceException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public static ServiceException NotFound(string message = "対象が見つかりません。") => new(StatusCodes.Status404NotFound, message);
    public static ServiceException Forbidden(string message = "この操作を行う権限がありません。") => new(StatusCodes.Status403Forbidden, message);
    public static ServiceException BadRequest(string message) => new(StatusCodes.Status400BadRequest, message);
    public static ServiceException Conflict(string message) => new(StatusCodes.Status409Conflict, message);
}
