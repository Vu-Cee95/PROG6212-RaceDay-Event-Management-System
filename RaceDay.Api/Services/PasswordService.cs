using Microsoft.AspNetCore.Identity;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

/// <summary>Hashes and verifies passwords. Plain-text passwords are never persisted.</summary>
public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string hash, string password);
}

/// <summary>
/// Wraps ASP.NET Core Identity's <see cref="PasswordHasher{TUser}"/> (PBKDF2 with
/// HMAC-SHA512, 100 000 iterations and a random per-user salt). The hasher ships
/// with the ASP.NET Core shared framework, so no extra NuGet package is required.
/// </summary>
public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<User> _hasher = new();
    private static readonly User Dummy = new();

    public string Hash(string password) => _hasher.HashPassword(Dummy, password);

    public bool Verify(string hash, string password)
    {
        if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(password))
        {
            return false;
        }

        try
        {
            var outcome = _hasher.VerifyHashedPassword(Dummy, hash, password);
            return outcome is PasswordVerificationResult.Success
                           or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            // Stored value is not a valid hash (e.g. corrupt data) - treat as a failed login.
            return false;
        }
    }
}
