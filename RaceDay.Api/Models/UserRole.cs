namespace RaceDay.Api.Models;

/// <summary>
/// The two roles supported by RaceDay. Stored in the Users.Role column as
/// NVARCHAR(20) with a CHECK constraint, exactly as in the Part 1 SQL script.
/// </summary>
public static class UserRole
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    public static readonly string[] All = { Organiser, Participant };

    public static bool IsValid(string? role) =>
        role is not null && All.Contains(role, StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns the canonical casing ("Organiser"/"Participant") for a valid role.</summary>
    public static string Normalise(string role) =>
        All.First(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
}
