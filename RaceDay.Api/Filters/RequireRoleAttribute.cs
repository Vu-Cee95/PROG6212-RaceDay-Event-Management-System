using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Api.Services;
using RaceDay.Contracts;

namespace RaceDay.Api.Filters;

// Blocks the request if the user is not logged in (401) or has the wrong role (403).
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _role;

    public RequireRoleAttribute(string role)
    {
        _role = role;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

        if (!currentUser.IsAuthenticated)
        {
            context.Result = new UnauthorizedObjectResult(new ApiError
            {
                Message = "You must be logged in to access this resource."
            });
            return;
        }

        if (!currentUser.IsInRole(_role))
        {
            context.Result = new ObjectResult(new ApiError
            {
                Message = $"This action requires the {_role} role. Your role is {currentUser.Role}."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
