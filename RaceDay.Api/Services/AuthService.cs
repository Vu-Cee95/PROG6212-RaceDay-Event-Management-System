using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Services;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request);
    void Logout();
}

/// <summary>
/// Registration, login and logout. On success the user's Id and Role are written
/// to the server-side session so that later requests can be authorised.
/// </summary>
public class AuthService : IAuthService
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly ICurrentUserService _currentUser;

    public AuthService(RaceDayDbContext db, IPasswordService passwords, ICurrentUserService currentUser)
    {
        _db = db;
        _passwords = passwords;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        if (!UserRole.IsValid(request.Role))
        {
            return ServiceResult<AuthResponse>.BadRequest(
                $"Role must be either '{UserRole.Organiser}' or '{UserRole.Participant}'.");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        // Email is UNIQUE in the database; check here as well so the API can return a
        // clear 400 instead of a database exception (and so the rule holds in tests).
        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            return ServiceResult<AuthResponse>.BadRequest("An account with this email address already exists.");
        }

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            Role = UserRole.Normalise(request.Role),
            PasswordHash = _passwords.Hash(request.Password),
            DateRegistered = DateTime.Now
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return ServiceResult<AuthResponse>.Created(ToResponse(user, "Registration successful. You can now log in."));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email);

        // Same message for unknown email and wrong password - do not leak which one failed.
        if (user is null || !_passwords.Verify(user.PasswordHash, request.Password))
        {
            return ServiceResult<AuthResponse>.Unauthorized("Invalid email address or password.");
        }

        _currentUser.SignIn(user.UserId, user.Role);

        return ServiceResult<AuthResponse>.Ok(ToResponse(user, $"Login successful. Welcome back, {user.FullName}."));
    }

    public void Logout() => _currentUser.SignOut();

    private static AuthResponse ToResponse(User user, string message) => new()
    {
        UserId = user.UserId,
        Email = user.Email,
        FullName = user.FullName,
        Role = user.Role,
        Message = message
    };
}
