using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Api.Tests.Infrastructure;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

/// <summary>User registration, login, logout and password storage.</summary>
public class AuthenticationTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public AuthenticationTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    // ---------------------------------------------------------------- Register

    [Theory]
    [InlineData(UserRole.Organiser)]
    [InlineData(UserRole.Participant)]
    public async Task Register_WithValidDetails_Returns201AndRole(string role)
    {
        using var client = _factory.CreateSessionClient();
        var email = ApiClientExtensions.UniqueEmail();

        var response = await client.RegisterAsync(email, role, fullName: "Naledi Khumalo");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.True(body!.UserId > 0);
        Assert.Equal(email, body.Email);
        Assert.Equal("Naledi Khumalo", body.FullName);
        Assert.Equal(role, body.Role);
    }

    [Fact]
    public async Task Register_RoleIsCaseInsensitive_StoresCanonicalRole()
    {
        using var client = _factory.CreateSessionClient();

        var response = await client.RegisterAsync(ApiClientExtensions.UniqueEmail(), "participant");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(UserRole.Participant, body!.Role);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400()
    {
        using var client = _factory.CreateSessionClient();
        var email = ApiClientExtensions.UniqueEmail();
        (await client.RegisterAsync(email, UserRole.Participant)).EnsureSuccessStatusCode();

        var response = await client.RegisterAsync(email, UserRole.Organiser);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.ReadErrorAsync();
        Assert.Contains("already exists", error.Message);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Runner")]
    [InlineData("")]
    public async Task Register_WithInvalidRole_Returns400(string role)
    {
        using var client = _factory.CreateSessionClient();

        var response = await client.RegisterAsync(ApiClientExtensions.UniqueEmail(), role);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithMissingFields_Returns400WithValidationErrors()
    {
        using var client = _factory.CreateSessionClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new { email = "not-an-email", password = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.ReadErrorAsync();
        Assert.NotNull(error.Errors);
        Assert.Contains("Email", error.Errors!.Keys);
        Assert.Contains("Password", error.Errors.Keys);
        Assert.Contains("FullName", error.Errors.Keys);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        using var client = _factory.CreateSessionClient();
        var email = ApiClientExtensions.UniqueEmail();
        const string password = "Sup3rSecret!";

        (await client.RegisterAsync(email, UserRole.Participant, password)).EnsureSuccessStatusCode();

        var stored = await _factory.QueryDbAsync(db => db.Users.SingleAsync(u => u.Email == email));
        Assert.NotEqual(password, stored.PasswordHash);
        Assert.DoesNotContain(password, stored.PasswordHash);
        Assert.True(new PasswordService().Verify(stored.PasswordHash, password));
    }

    // ---------------------------------------------------------------- Login

    [Fact]
    public async Task Login_WithValidCredentials_Returns200AndUserDetails()
    {
        using var client = _factory.CreateSessionClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = ApiClientExtensions.SeedOrganiserEmail, Password = ApiClientExtensions.SeedPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(UserRole.Organiser, body!.Role);
        Assert.Equal("Thabo Mokoena", body.FullName);
        Assert.Contains(response.Headers, h => h.Key == "Set-Cookie" && h.Value.Any(v => v.Contains(".RaceDay.Session")));
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        using var client = _factory.CreateSessionClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = ApiClientExtensions.SeedParticipantEmail, Password = "WrongPassword!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        using var client = _factory.CreateSessionClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "nobody@raceday.co.za", Password = ApiClientExtensions.SeedPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithMissingBody_Returns400()
    {
        using var client = _factory.CreateSessionClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_CreatesSession_ThatAuthenticatesSubsequentRequests()
    {
        using var client = _factory.CreateSessionClient();
        var login = await client.LoginAsSeedParticipantAsync();

        var profile = await client.GetFromJsonAsync<ProfileDto>("/api/users/profile");

        Assert.Equal(login.UserId, profile!.UserId);
        Assert.Equal(UserRole.Participant, profile.Role);
    }

    [Fact]
    public async Task Login_SessionIsPerClient_NotShared()
    {
        using var loggedIn = _factory.CreateSessionClient();
        using var anonymous = _factory.CreateSessionClient();
        await loggedIn.LoginAsSeedParticipantAsync();

        var response = await anonymous.GetAsync("/api/users/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------------------------------------------------------------- Logout

    [Fact]
    public async Task Logout_ClearsSession_ProtectedEndpointReturns401Afterwards()
    {
        using var client = _factory.CreateSessionClient();
        await client.LoginAsSeedParticipantAsync();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/profile")).StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", null);
        var afterLogout = await client.GetAsync("/api/users/profile");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task Logout_WhenNotLoggedIn_StillReturns204()
    {
        using var client = _factory.CreateSessionClient();

        var response = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
