using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ShareMaster.Server.Services;

/// <summary>ServiceException を ProblemDetails 形式のレスポンスに変換する</summary>
public sealed class ServiceExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not ServiceException ex) return false;
        context.Response.StatusCode = ex.StatusCode;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = ex,
            ProblemDetails = new ProblemDetails { Status = ex.StatusCode, Title = ex.Message },
        });
    }
}
