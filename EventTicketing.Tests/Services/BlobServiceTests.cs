using EventTicketing.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventTicketing.Tests.Services;

/// <summary>
/// Unit tests for BlobService. The happy-path (SAS URL generation) requires a real
/// Azure storage account and is covered by the integration tests via FakeBlobService.
/// These tests focus on the configuration-guard behaviour.
/// </summary>
public class BlobServiceTests
{
    private static BlobService BuildService(
        string? connectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net",
        string? containerName = "events")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureBlob:ConnectionString"] = connectionString,
                ["AzureBlob:ContainerName"] = containerName,
            })
            .Build();

        return new BlobService(config, NullLogger<BlobService>.Instance);
    }

    [Fact]
    public async Task GenerateUploadSasUrlAsync_MissingConnectionString_ThrowsInvalidOperationException()
    {
        var sut = BuildService(connectionString: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GenerateUploadSasUrlAsync("banner.jpg", "image/jpeg"));
    }

    [Fact]
    public async Task GenerateUploadSasUrlAsync_MissingContainerName_ThrowsInvalidOperationException()
    {
        var sut = BuildService(containerName: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GenerateUploadSasUrlAsync("banner.jpg", "image/jpeg"));
    }
}
