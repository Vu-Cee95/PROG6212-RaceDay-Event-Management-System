using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Infrastructure;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

// Both roles can view and update their own profile - and only their own.
public class ProfileTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public ProfileTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProfile_ReturnsTheLoggedInUsersOwnDetails()
    {
        using var client = _factory.CreateSessionClient();
        var login = await client.RegisterAndLoginAsync(UserRole.Organiser, fullName: "Zanele Mthembu");

        var profile = await client.GetFromJsonAsync<ProfileDto>("/api/users/profile");

        Assert.NotNull(profile);
        Assert.Equal(login.UserId, profile!.UserId);
        Assert.Equal(login.Email, profile.Email);
        Assert.Equal("Zanele Mthembu", profile.FullName);
        Assert.Equal(UserRole.Organiser, profile.Role);
        Assert.Null(profile.ProfilePictureUrl);
    }

    [Fact]
    public async Task UpdateProfile_ChangesNameAndPicture_AndPersists()
    {
        using var client = _factory.CreateSessionClient();
        var login = await client.RegisterAndLoginParticipantAsync();

        var response = await client.PutAsJsonAsync("/api/users/profile", new UpdateProfileRequest
        {
            FullName = "Updated Name",
            ProfilePictureUrl = "https://cdn.raceday.co.za/profiles/updated.png"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProfileDto>();
        Assert.Equal("Updated Name", body!.FullName);
        Assert.Equal("https://cdn.raceday.co.za/profiles/updated.png", body.ProfilePictureUrl);

        var stored = await _factory.QueryDbAsync(db => db.Users.SingleAsync(u => u.UserId == login.UserId));
        Assert.Equal("Updated Name", stored.FullName);
        Assert.Equal(login.Email, stored.Email);      // email unchanged
        Assert.Equal(UserRole.Participant, stored.Role); // role unchanged
    }

    [Fact]
    public async Task UpdateProfile_DoesNotAffectOtherUsers()
    {
        using var alice = _factory.CreateSessionClient();
        using var bob = _factory.CreateSessionClient();
        var aliceLogin = await alice.RegisterAndLoginAsync(UserRole.Participant, "Alice Original");
        var bobLogin = await bob.RegisterAndLoginAsync(UserRole.Participant, "Bob Original");

        await bob.PutAsJsonAsync("/api/users/profile", new UpdateProfileRequest { FullName = "Bob Changed" });

        var aliceProfile = await alice.GetFromJsonAsync<ProfileDto>("/api/users/profile");
        Assert.Equal("Alice Original", aliceProfile!.FullName);
        Assert.Equal(aliceLogin.UserId, aliceProfile.UserId);
        Assert.NotEqual(bobLogin.UserId, aliceProfile.UserId);
    }

    [Fact]
    public async Task UpdateProfile_WithEmptyName_Returns400()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginParticipantAsync();

        var response = await client.PutAsJsonAsync("/api/users/profile", new UpdateProfileRequest { FullName = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.ReadErrorAsync();
        Assert.Contains("FullName", error.Errors!.Keys);
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidPictureUrl_Returns400()
    {
        using var client = _factory.CreateSessionClient();
        await client.RegisterAndLoginParticipantAsync();

        var response = await client.PutAsJsonAsync("/api/users/profile",
            new UpdateProfileRequest { FullName = "Valid Name", ProfilePictureUrl = "not a url" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithoutLogin_Returns401()
    {
        using var client = _factory.CreateSessionClient();

        var response = await client.PutAsJsonAsync("/api/users/profile", new UpdateProfileRequest { FullName = "Anyone" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
