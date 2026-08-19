using EventTicketing.Api.Services;

namespace EventTicketing.Tests.Fakes;

public class FakeBlobService : IBlobService
{
    public record SasCall(string FileName, string ContentType);

    public List<SasCall> Calls { get; } = [];

    /// <summary>Fixed upload URL returned to callers. Tests can assert on it.</summary>
    public string FakeUploadUrl { get; set; } = "https://fake.blob.core.windows.net/events/fake-blob?sas=token";

    /// <summary>Fixed public URL returned to callers. Tests can assert on it.</summary>
    public string FakePublicUrl { get; set; } = "https://fake.blob.core.windows.net/events/fake-blob";

    public Task<BlobSasResult> GenerateUploadSasUrlAsync(
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        Calls.Add(new(fileName, contentType));
        return Task.FromResult(new BlobSasResult(FakeUploadUrl, FakePublicUrl));
    }
}
