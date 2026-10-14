namespace RaceDay.Api.Services;

/// <summary>
/// Exposes the identity of the caller as stored in the server-side session.
/// After login the session holds the UserId and Role; every subsequent request
/// carries the session cookie, so the API can resolve "who is calling" without
/// the client re-sending credentials.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>True when the session contains a logged-in user.</summary>
    bool IsAuthenticated { get; }

    /// <summary>UserId stored in the session, or null when not logged in.</summary>
    int? UserId { get; }

    /// <summary>Role ("Organiser"/"Participant") stored in the session, or null.</summary>
    string? Role { get; }

    bool IsInRole(string role);

    /// <summary>Writes the user's identity and role into the session (called on login/register).</summary>
    void SignIn(int userId, string role);

    /// <summary>Clears the session (called on logout).</summary>
    void SignOut();
}

public class SessionCurrentUserService : ICurrentUserService
{
    public const string UserIdKey = "UserId";
    public const string RoleKey = "Role";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionCurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ISession? Session => _httpContextAccessor.HttpContext?.Session;

    public int? UserId => Session?.GetInt32(UserIdKey);

    public string? Role => Session?.GetString(RoleKey);

    public bool IsAuthenticated => UserId.HasValue && !string.IsNullOrEmpty(Role);

    public bool IsInRole(string role) =>
        IsAuthenticated && string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);

    public void SignIn(int userId, string role)
    {
        var session = Session ?? throw new InvalidOperationException("Session is not available.");
        session.SetInt32(UserIdKey, userId);
        session.SetString(RoleKey, role);
    }

    public void SignOut() => Session?.Clear();
}
