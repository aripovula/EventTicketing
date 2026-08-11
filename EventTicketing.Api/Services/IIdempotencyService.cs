using EventTicketing.Api.Models;

namespace EventTicketing.Api.Services;

public interface IIdempotencyService
{
    /// <summary>
    /// Returns a cached response if one exists for the given key + path and has not expired.
    /// Returns null if no valid cached response is found.
    /// </summary>
    Task<IdempotencyKey?> GetAsync(string key, string requestPath);

    /// <summary>
    /// Stores the response for the given key + path with a 24-hour TTL.
    /// </summary>
    Task StoreAsync(string key, string requestPath, int statusCode, string responseBody);
}
