namespace RaceDay.Api.Services;

// Exposes the identity of the caller as stored in the server-side session.
public interface ICurrentUserService
{
    // True when the session contains a logged-in user.
    bool IsAuthenticated { get; }

    // UserId stored in the session, or null when not logged in.
    int? UserId { get; }

    // Role ("Organiser"/"Participant") stored in the session, or null.
    string? Role { get; }

    bool IsInRole(string role);

    // Writes the user's identity and role into the session (called on login/register).
    void SignIn(int userId, string role);

    // Clears the session (called on logout).
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
