namespace EventTicketing.Api.Services;

/// <summary>
/// Abstracts Azure Blob Storage so the upload flow is testable without real network calls.
/// The concrete implementation uses a short-lived SAS URL so the browser can PUT the file
/// directly to Blob Storage — raw file data never passes through the API server.
/// </summary>
public interface IBlobService
{
    /// <summary>
    /// Generates a write-only SAS URL (5-minute TTL) for a single blob upload and returns
    /// the public read URL that will point to the blob once uploaded.
    /// </summary>
    /// <param name="fileName">
    /// Original file name supplied by the client. Used to derive the blob name and to
    /// set the <c>Content-Disposition</c> header on the SAS token.
    /// </param>
    /// <param name="contentType">MIME type (e.g. <c>image/jpeg</c>).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="BlobSasResult"/> containing the pre-signed upload URL and the
    /// permanent public read URL to store on the event record.
    /// </returns>
    Task<BlobSasResult> GenerateUploadSasUrlAsync(
        string fileName,
        string contentType,
        CancellationToken ct = default);
}

/// <summary>SAS URL pair returned for a single blob upload slot.</summary>
/// <param name="UploadUrl">
/// Write-only SAS URL. The browser should PUT the file to this URL directly.
/// Expires after 5 minutes.
/// </param>
/// <param name="PublicUrl">
/// The permanent public read URL to store on the event record after upload completes.
/// </param>
public record BlobSasResult(string UploadUrl, string PublicUrl);
