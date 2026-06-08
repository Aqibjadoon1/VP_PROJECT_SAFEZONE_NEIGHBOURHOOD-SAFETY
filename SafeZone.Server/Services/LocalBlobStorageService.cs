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
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (content is null)
            throw new ArgumentNullException(nameof(content));

        var blobId = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(_storageRoot, blobId);
        await using var fileStream = File.Create(filePath);
        await content.CopyToAsync(fileStream);
        return blobId;
    }

    public Task<Stream?> DownloadAsync(string blobId)
    {
        var filePath = GetSafePath(blobId);
        if (filePath is null || !File.Exists(filePath)) return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(File.OpenRead(filePath));
    }

    public Task<bool> DeleteAsync(string blobId)
    {
        var filePath = GetSafePath(blobId);
        if (filePath is null || !File.Exists(filePath)) return Task.FromResult(false);
        File.Delete(filePath);
        return Task.FromResult(true);
    }

    public string GetPublicUrl(string blobId)
    {
        return $"/uploads/{blobId}";
    }

    private string? GetSafePath(string blobId)
    {
        var filePath = Path.GetFullPath(Path.Combine(_storageRoot, blobId));
        var rootPath = Path.GetFullPath(_storageRoot);

        if (!filePath.StartsWith(rootPath + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !filePath.Equals(rootPath, StringComparison.Ordinal))
        {
            return null;
        }

        return filePath;
    }
}
