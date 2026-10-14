using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Api.Services;
using RaceDay.Contracts;

namespace RaceDay.Api.Filters;

/// <summary>
/// Rejects requests that do not carry a logged-in session with
/// 401 Unauthorized. Apply to any non-public endpoint that both roles may use.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireAuthenticatedAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

        if (!currentUser.IsAuthenticated)
        {
            context.Result = new UnauthorizedObjectResult(new ApiError
            {
                Message = "You must be logged in to access this resource."
            });
        }
    }
}
