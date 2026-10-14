namespace RaceDay.Api.Models;

// Allowed values for Enrolments.Status (NVARCHAR(20), default 'Pending').
public static class EnrolmentStatus
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = { Pending, Confirmed, Cancelled };

    public static bool IsValid(string? status) =>
        status is not null && All.Contains(status, StringComparer.OrdinalIgnoreCase);

    public static string Normalise(string status) =>
        All.First(s => s.Equals(status, StringComparison.OrdinalIgnoreCase));
}
