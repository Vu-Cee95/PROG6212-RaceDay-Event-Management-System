using Microsoft.AspNetCore.Http;

namespace RaceDay.Api.Services;

// Shared rules for event banners and profile pictures.
public static class ImageUpload
{
    public const int MaxBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return "Choose an image file to upload.";
        }

        if (file.Length > MaxBytes)
        {
            return "Images must be 5 MB or smaller.";
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            return "Only JPG, PNG, WEBP and GIF images are allowed.";
        }

        if (string.IsNullOrWhiteSpace(file.ContentType) || !AllowedContentTypes.Contains(file.ContentType))
        {
            return "Only JPG, PNG, WEBP and GIF images are allowed.";
        }

        if (!LooksLikeImage(file))
        {
            return "The file is not a valid image.";
        }

        return null;
    }

    public static string SafeExtension(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        return AllowedExtensions.Contains(extension) ? extension.ToLowerInvariant() : ".jpg";
    }

    private static bool LooksLikeImage(IFormFile file)
    {
        Span<byte> header = stackalloc byte[12];
        using var stream = file.OpenReadStream();
        var read = stream.Read(header);
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        if (read < 4)
        {
            return false;
        }

        // JPEG
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return true;
        }

        // PNG
        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
        {
            return true;
        }

        // GIF
        if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46)
        {
            return true;
        }

        // WEBP: RIFF....WEBP
        if (read >= 12
            && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
            && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
        {
            return true;
        }

        return false;
    }
}
