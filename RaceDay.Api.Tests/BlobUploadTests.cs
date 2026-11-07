using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.Api.Tests.Infrastructure;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

public class BlobUploadTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public BlobUploadTests(RaceDayApiFactory factory) => _factory = factory;

    [Fact]
    public async Task UploadBanner_AsOwner_StoresUrlOnEvent()
    {
        var client = _factory.CreateSessionClient();
        await client.LoginAsSeedOrganiserAsync();
        var created = await client.CreateEventAsync("Banner Event");

        using var content = JpegForm("file", "banner.jpg");
        var response = await client.PostAsync($"/api/events/{created.EventId}/banner", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto.BannerImageUrl));
        Assert.Contains("event-banners", dto.BannerImageUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("raceday-images", dto.BannerImageUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadBanner_AsParticipant_Returns403()
    {
        var organiser = _factory.CreateSessionClient();
        await organiser.LoginAsSeedOrganiserAsync();
        var created = await organiser.CreateEventAsync("Locked Banner");

        var participant = _factory.CreateSessionClient();
        await participant.LoginAsSeedParticipantAsync();
        using var content = JpegForm("file", "banner.jpg");
        var response = await participant.PostAsync($"/api/events/{created.EventId}/banner", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_Anonymous_Returns401()
    {
        var client = _factory.CreateSessionClient();
        using var content = JpegForm("file", "banner.jpg");
        var response = await client.PostAsync("/api/events/1/banner", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_OtherOrganiser_Returns403()
    {
        var owner = _factory.CreateSessionClient();
        await owner.LoginAsSeedOrganiserAsync();
        var created = await owner.CreateEventAsync("Not Yours");

        var other = _factory.CreateSessionClient();
        await other.LoginAsync(ApiClientExtensions.SeedOtherOrganiserEmail);
        using var content = JpegForm("file", "banner.jpg");
        var response = await other.PostAsync($"/api/events/{created.EventId}/banner", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_TextFile_Returns400()
    {
        var client = _factory.CreateSessionClient();
        await client.LoginAsSeedOrganiserAsync();
        var created = await client.CreateEventAsync("Bad File");

        using var form = new MultipartFormDataContent();
        var bytes = new ByteArrayContent("not an image"u8.ToArray());
        bytes.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(bytes, "file", "notes.txt");

        var response = await client.PostAsync($"/api/events/{created.EventId}/banner", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_EmptyFile_Returns400()
    {
        var client = _factory.CreateSessionClient();
        await client.LoginAsSeedOrganiserAsync();
        var created = await client.CreateEventAsync("Empty File");

        using var form = new MultipartFormDataContent();
        var bytes = new ByteArrayContent(Array.Empty<byte>());
        bytes.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(bytes, "file", "empty.jpg");

        var response = await client.PostAsync($"/api/events/{created.EventId}/banner", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_AsLoggedInUser_StoresUrl()
    {
        var client = _factory.CreateSessionClient();
        await client.LoginAsSeedParticipantAsync();

        using var content = JpegForm("file", "me.jpg");
        var response = await client.PostAsync("/api/users/profile/picture", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<ProfileDto>();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto.ProfilePictureUrl));
        Assert.Contains("profile-pictures", dto.ProfilePictureUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadProfilePicture_Anonymous_Returns401()
    {
        var client = _factory.CreateSessionClient();
        using var content = JpegForm("file", "me.jpg");
        var response = await client.PostAsync("/api/users/profile/picture", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static MultipartFormDataContent JpegForm(string field, string fileName)
    {
        var form = new MultipartFormDataContent();
        // Minimal JPEG SOI + APP0 marker so ImageUpload accepts the bytes.
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0xFF, 0xD9 };
        var bytes = new ByteArrayContent(jpeg);
        bytes.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(bytes, field, fileName);
        return form;
    }
}
