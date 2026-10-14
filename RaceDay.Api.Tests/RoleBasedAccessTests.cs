using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests;

/// <summary>
/// Proves that every protected endpoint rejects anonymous callers (401) and that
/// Organiser-only / Participant-only endpoints reject the other role (403).
/// </summary>
public class RoleBasedAccessTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public RoleBasedAccessTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    private static HttpRequestMessage Request(string method, string url) =>
        new(new HttpMethod(method), url)
        {
            Content = method is "POST" or "PUT" ? JsonContent.Create(new { }) : null
        };

    // ------------------------------------------------ Anonymous -> 401

    public static IEnumerable<object[]> ProtectedEndpoints => new[]
    {
        new object[] { "GET", "/api/users/profile" },
        new object[] { "PUT", "/api/users/profile" },
        new object[] { "GET", "/api/events/mine" },
        new object[] { "POST", "/api/events" },
        new object[] { "PUT", "/api/events/1" },
        new object[] { "DELETE", "/api/events/1" },
        new object[] { "POST", "/api/events/1/categories" },
        new object[] { "PUT", "/api/categories/1" },
        new object[] { "DELETE", "/api/categories/1" },
        new object[] { "POST", "/api/enrolments" },
        new object[] { "GET", "/api/users/3/enrolments" },
        new object[] { "GET", "/api/events/1/enrolments" },
        new object[] { "PUT", "/api/enrolments/1/status" },
        new object[] { "POST", "/api/enrolments/1/result" },
        new object[] { "GET", "/api/users/3/results" },
        new object[] { "GET", "/api/events/1/results" },
    };

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task ProtectedEndpoint_WithoutLogin_Returns401(string method, string url)
    {
        using var client = _factory.CreateSessionClient();

        var response = await client.SendAsync(Request(method, url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ------------------------------------------------ Public endpoints -> 200 without login

    [Theory]
    [InlineData("/api/events")]
    [InlineData("/api/events/1")]
    [InlineData("/api/events/1/categories")]
    [InlineData("/api/eventtypes")]
    public async Task PublicEndpoint_WithoutLogin_Returns200(string url)
    {
        using var client = _factory.CreateSessionClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ------------------------------------------------ Participant -> Organiser endpoints -> 403

    public static IEnumerable<object[]> OrganiserOnlyEndpoints => new[]
    {
        new object[] { "GET", "/api/events/mine" },
        new object[] { "POST", "/api/events" },
        new object[] { "PUT", "/api/events/1" },
        new object[] { "DELETE", "/api/events/1" },
        new object[] { "POST", "/api/events/1/categories" },
        new object[] { "PUT", "/api/categories/1" },
        new object[] { "DELETE", "/api/categories/1" },
        new object[] { "GET", "/api/events/1/enrolments" },
        new object[] { "PUT", "/api/enrolments/1/status" },
        new object[] { "POST", "/api/enrolments/1/result" },
        new object[] { "GET", "/api/events/1/results" },
    };

    [Theory]
    [MemberData(nameof(OrganiserOnlyEndpoints))]
    public async Task OrganiserOnlyEndpoint_AsParticipant_Returns403(string method, string url)
    {
        using var client = _factory.CreateSessionClient();
        await client.LoginAsSeedParticipantAsync();

        var response = await client.SendAsync(Request(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await response.ReadErrorAsync();
        Assert.Contains("Organiser role", error.Message);
    }

    // ------------------------------------------------ Organiser -> Participant endpoints -> 403

    public static IEnumerable<object[]> ParticipantOnlyEndpoints => new[]
    {
        new object[] { "POST", "/api/enrolments" },
        new object[] { "GET", "/api/users/1/enrolments" },
        new object[] { "GET", "/api/users/1/results" },
    };

    [Theory]
    [MemberData(nameof(ParticipantOnlyEndpoints))]
    public async Task ParticipantOnlyEndpoint_AsOrganiser_Returns403(string method, string url)
    {
        using var client = _factory.CreateSessionClient();
        await client.LoginAsSeedOrganiserAsync();

        var response = await client.SendAsync(Request(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await response.ReadErrorAsync();
        Assert.Contains("Participant role", error.Message);
    }

    // ------------------------------------------------ Shared endpoints -> both roles allowed

    [Theory]
    [InlineData(ApiClientExtensions.SeedOrganiserEmail)]
    [InlineData(ApiClientExtensions.SeedParticipantEmail)]
    public async Task ProfileEndpoint_IsAvailableToBothRoles(string email)
    {
        using var client = _factory.CreateSessionClient();
        await client.LoginAsync(email);

        var response = await client.GetAsync("/api/users/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
