using System.Net.Http.Json;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests.Infrastructure;

/// <summary>Helpers that drive the API the way a real client would.</summary>
public static class ApiClientExtensions
{
    public const string SeedPassword = "Password123!";
    public const string SeedOrganiserEmail = "thabo.mokoena@raceday.co.za";      // UserId 1, owns events 1 & 2
    public const string SeedOtherOrganiserEmail = "sarah.vandermerwe@raceday.co.za"; // UserId 2, owns event 3
    public const string SeedParticipantEmail = "lwando.dlamini@gmail.com";        // UserId 3
    public const string SeedOtherParticipantEmail = "priya.naidoo@gmail.com";     // UserId 4

    public static string UniqueEmail(string prefix = "user") =>
        $"{prefix}.{Guid.NewGuid():N}@test.raceday.co.za";

    public static Task<HttpResponseMessage> RegisterAsync(this HttpClient client, string email, string role,
        string password = SeedPassword, string fullName = "Test User") =>
        client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = password,
            FullName = fullName,
            Role = role
        });

    public static async Task<AuthResponse> LoginAsync(this HttpClient client, string email, string password = SeedPassword)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    public static Task<AuthResponse> LoginAsSeedOrganiserAsync(this HttpClient client) =>
        client.LoginAsync(SeedOrganiserEmail);

    public static Task<AuthResponse> LoginAsSeedParticipantAsync(this HttpClient client) =>
        client.LoginAsync(SeedParticipantEmail);

    /// <summary>Registers a brand-new user with the given role and logs them in.</summary>
    public static async Task<AuthResponse> RegisterAndLoginAsync(this HttpClient client, string role, string? fullName = null)
    {
        var email = UniqueEmail(role.ToLowerInvariant());
        var register = await client.RegisterAsync(email, role, fullName: fullName ?? $"Test {role}");
        register.EnsureSuccessStatusCode();
        return await client.LoginAsync(email);
    }

    public static Task<AuthResponse> RegisterAndLoginOrganiserAsync(this HttpClient client) =>
        client.RegisterAndLoginAsync(UserRole.Organiser);

    public static Task<AuthResponse> RegisterAndLoginParticipantAsync(this HttpClient client) =>
        client.RegisterAndLoginAsync(UserRole.Participant);

    public static CreateEventRequest NewEventRequest(string? name = null) => new()
    {
        Name = name ?? $"Test Event {Guid.NewGuid():N}"[..30],
        Description = "Created by an automated test.",
        EventDate = new DateTime(2027, 5, 1, 6, 0, 0),
        Location = "Pretoria, Gauteng",
        Distance = 21.10m,
        EventTypeId = 1
    };

    /// <summary>Creates an event as the currently logged-in organiser.</summary>
    public static async Task<EventDto> CreateEventAsync(this HttpClient client, string? name = null)
    {
        var response = await client.PostAsJsonAsync("/api/events", NewEventRequest(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDto>())!;
    }

    public static async Task<CategoryDto> CreateCategoryAsync(this HttpClient client, int eventId, string name = "Open")
    {
        var response = await client.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new CategoryRequest { CategoryName = name, Description = $"{name} category" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    public static async Task<EnrolmentDto> EnrolAsync(this HttpClient client, int eventId, int categoryId)
    {
        var response = await client.PostAsJsonAsync("/api/enrolments",
            new CreateEnrolmentRequest { EventId = eventId, CategoryId = categoryId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EnrolmentDto>())!;
    }

    public static async Task<ApiError> ReadErrorAsync(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ApiError>())!;
}
