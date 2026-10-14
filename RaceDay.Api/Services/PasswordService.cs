using Microsoft.AspNetCore.Identity;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

// Hashes and verifies passwords. Plain-text passwords are never persisted.
public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string hash, string password);
}

// Uses ASP.NET Identity's PasswordHasher so each password gets its own salt.
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
