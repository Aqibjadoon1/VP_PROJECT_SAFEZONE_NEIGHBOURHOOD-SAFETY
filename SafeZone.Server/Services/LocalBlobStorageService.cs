using Microsoft.Extensions.Configuration;

namespace SafeZone.Server.Services;

public sealed class LocalBlobStorageService : IBlobStorageService
{
    private readonly string _storageRoot;

    public LocalBlobStorageService(IConfiguration configuration)
    {
        _storageRoot = configuration["BlobStorage:LocalPath"]
                       ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<string> UploadAsync(string fileName, Stream content, string? contentType = null)
    {
        var blobId = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(_storageRoot, blobId);
        await using var fileStream = File.Create(filePath);
        await content.CopyToAsync(fileStream);
        return blobId;
    }

    public Task<Stream?> DownloadAsync(string blobId)
    {
        var filePath = Path.Combine(_storageRoot, blobId);
        if (!File.Exists(filePath)) return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(File.OpenRead(filePath));
    }

    public Task<bool> DeleteAsync(string blobId)
    {
        var filePath = Path.Combine(_storageRoot, blobId);
        if (!File.Exists(filePath)) return Task.FromResult(false);
        File.Delete(filePath);
        return Task.FromResult(true);
    }

    public string GetPublicUrl(string blobId)
    {
        return $"/uploads/{blobId}";
    }
}
