namespace RaceDay.Api.Models;

// The two roles supported by RaceDay.
public static class UserRole
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    public static readonly string[] All = { Organiser, Participant };

    public static bool IsValid(string? role) =>
        role is not null && All.Contains(role, StringComparer.OrdinalIgnoreCase);

    // Returns the canonical casing ("Organiser"/"Participant") for a valid role.
    public static string Normalise(string role) =>
        All.First(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
}
