using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

/// <summary>
/// Unit tests for the authorisation filters in isolation, using a fake session
/// user. These prove the 401/403 decisions independently of any controller.
/// </summary>
public class RoleFilterTests
{
    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public int? UserId { get; init; }
        public string? Role { get; init; }
        public bool IsAuthenticated => UserId.HasValue && Role is not null;
        public bool IsInRole(string role) => IsAuthenticated && string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);
        public void SignIn(int userId, string role) => throw new NotSupportedException();
        public void SignOut() => throw new NotSupportedException();
    }

    private static AuthorizationFilterContext BuildContext(ICurrentUserService currentUser)
    {
        var services = new ServiceCollection();
        services.AddSingleton(currentUser);

        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private static int? StatusOf(IActionResult? result) => result switch
    {
        null => null,
        ObjectResult o => o.StatusCode,
        StatusCodeResult s => s.StatusCode,
        _ => throw new InvalidOperationException("Unexpected result type")
    };

    [Fact]
    public void RequireAuthenticated_AnonymousUser_Returns401()
    {
        var context = BuildContext(new FakeCurrentUser());

        new RequireAuthenticatedAttribute().OnAuthorization(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, StatusOf(context.Result));
    }

    [Theory]
    [InlineData(UserRole.Organiser)]
    [InlineData(UserRole.Participant)]
    public void RequireAuthenticated_LoggedInUserOfAnyRole_Passes(string role)
    {
        var context = BuildContext(new FakeCurrentUser { UserId = 7, Role = role });

        new RequireAuthenticatedAttribute().OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void RequireRole_AnonymousUser_Returns401()
    {
        var context = BuildContext(new FakeCurrentUser());

        new RequireRoleAttribute(UserRole.Organiser).OnAuthorization(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, StatusOf(context.Result));
    }

    [Theory]
    [InlineData(UserRole.Participant, UserRole.Organiser)]
    [InlineData(UserRole.Organiser, UserRole.Participant)]
    public void RequireRole_WrongRole_Returns403(string actualRole, string requiredRole)
    {
        var context = BuildContext(new FakeCurrentUser { UserId = 7, Role = actualRole });

        new RequireRoleAttribute(requiredRole).OnAuthorization(context);

        Assert.Equal(StatusCodes.Status403Forbidden, StatusOf(context.Result));
    }

    [Theory]
    [InlineData(UserRole.Organiser)]
    [InlineData(UserRole.Participant)]
    public void RequireRole_CorrectRole_Passes(string role)
    {
        var context = BuildContext(new FakeCurrentUser { UserId = 7, Role = role });

        new RequireRoleAttribute(role).OnAuthorization(context);

        Assert.Null(context.Result);
    }
}
