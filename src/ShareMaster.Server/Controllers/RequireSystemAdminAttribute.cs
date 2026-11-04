using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ShareMaster.Server.Auth;

namespace ShareMaster.Server.Controllers;

/// <summary>システム管理者のみ実行可能なアクションに付与する</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireSystemAdminAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();
        if (!await user.IsSystemAdminAsync(context.HttpContext.RequestAborted))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "システム管理者のみ実行できます。",
            }) { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }
        await next();
    }
}
