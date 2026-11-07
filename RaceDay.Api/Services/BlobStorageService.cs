using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public interface IBlobStorageService
{
    Task<string> UploadAsync(IFormFile file, string folder);
}

// Uploads images to Azure Blob Storage. MVC never talks to Azure; only this API service does.
public class BlobStorageService : IBlobStorageService
{
    private readonly string _connectionString;
    private readonly string _containerName;

    public BlobStorageService(IConfiguration configuration)
    {
        _connectionString = configuration["AzureBlobStorage:ConnectionString"] ?? string.Empty;
        _containerName = configuration["AzureBlobStorage:ContainerName"] ?? "raceday-images";
    }

    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Azure Blob Storage is not configured. Set AzureBlobStorage:ConnectionString via user secrets or an environment variable.");
        }

        var safeFolder = string.IsNullOrWhiteSpace(folder)
            ? "uploads"
            : folder.Replace('\\', '/').Trim('/');

        var blobName = $"{safeFolder}/{Guid.NewGuid():N}{ImageUpload.SafeExtension(file)}";
        var container = new BlobContainerClient(_connectionString, _containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var blob = container.GetBlobClient(blobName);
        await using var incoming = file.OpenReadStream();
        await using var buffer = new MemoryStream();
        await incoming.CopyToAsync(buffer);
        buffer.Position = 0;
        await blob.UploadAsync(buffer, new BlobHttpHeaders
        {
            ContentType = file.ContentType
        });

        return blob.Uri.ToString();
    }
}

// Used by API tests so uploads do not need a real Azure account.
public class FakeBlobStorageService : IBlobStorageService
{
    public Task<string> UploadAsync(IFormFile file, string folder)
    {
        var name = $"{folder.Trim('/')}/{Guid.NewGuid():N}{ImageUpload.SafeExtension(file)}";
        return Task.FromResult($"https://raceday.blob.core.windows.net/raceday-images/{name}");
    }
}
