using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace EventTicketing.Api.Services;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IBlobService"/>.
/// Configured via <c>AzureBlob:ConnectionString</c> and <c>AzureBlob:ContainerName</c>
/// in user-secrets / environment variables.
/// </summary>
public class BlobService(IConfiguration configuration, ILogger<BlobService> logger) : IBlobService
{
    private static readonly TimeSpan SasTtl = TimeSpan.FromMinutes(5);

    public Task<BlobSasResult> GenerateUploadSasUrlAsync(
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        var connectionString = configuration["AzureBlob:ConnectionString"]
            ?? throw new InvalidOperationException("AzureBlob:ConnectionString is not configured.");
        var containerName = configuration["AzureBlob:ContainerName"]
            ?? throw new InvalidOperationException("AzureBlob:ContainerName is not configured.");

        // Prefix with a GUID so concurrent uploads of the same file name don't clash.
        var blobName = $"{Guid.NewGuid():N}_{fileName}";

        var serviceClient = new BlobServiceClient(connectionString);
        var containerClient = serviceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(SasTtl),
            ContentType = contentType,
        };

        // Write-only: the browser can PUT the blob but cannot read or list blobs.
        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        var uploadUrl = blobClient.GenerateSasUri(sasBuilder).ToString();

        // Public read URL — the container must have public read access enabled
        // (or the Event record stores this URL for later SAS-signed reads).
        var publicUrl = blobClient.Uri.ToString();

        logger.LogInformation(
            "Generated SAS upload URL for blob {BlobName} in container {Container} (TTL {Ttl})",
            blobName, containerName, SasTtl);

        return Task.FromResult(new BlobSasResult(uploadUrl, publicUrl));
    }
}
