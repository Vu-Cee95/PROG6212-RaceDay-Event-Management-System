using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

// Pure unit tests for the password hashing service.
public class PasswordServiceTests
{
    private readonly PasswordService _service = new();

    [Fact]
    public void Hash_DoesNotContainPlainTextPassword()
    {
        const string password = "RaceDay2026!";

        var hash = _service.Hash(password);

        Assert.NotEqual(password, hash);
        Assert.DoesNotContain(password, hash);
        Assert.True(hash.Length > 40, "A PBKDF2 hash with salt should be a long Base64 string.");
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes_BecauseOfRandomSalt()
    {
        var first = _service.Hash("RaceDay2026!");
        var second = _service.Hash("RaceDay2026!");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _service.Hash("RaceDay2026!");

        Assert.True(_service.Verify(hash, "RaceDay2026!"));
    }

    [Theory]
    [InlineData("raceday2026!")]
    [InlineData("RaceDay2026")]
    [InlineData("")]
    public void Verify_WithWrongPassword_ReturnsFalse(string attempt)
    {
        var hash = _service.Hash("RaceDay2026!");

        Assert.False(_service.Verify(hash, attempt));
    }

    [Fact]
    public void Verify_WithEmptyOrCorruptHash_ReturnsFalse()
    {
        Assert.False(_service.Verify(string.Empty, "RaceDay2026!"));
        Assert.False(_service.Verify("not-a-real-hash", "RaceDay2026!"));
    }
}
