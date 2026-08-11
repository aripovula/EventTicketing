using EventTicketing.Api.Data;
using EventTicketing.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Services;

public class IdempotencyService(AppDbContext db) : IIdempotencyService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    public async Task<IdempotencyKey?> GetAsync(string key, string requestPath)
    {
        var entry = await db.IdempotencyKeys
            .FirstOrDefaultAsync(i => i.Key == key && i.RequestPath == requestPath);

        if (entry is null || entry.ExpiresAt < DateTime.UtcNow)
            return null;

        return entry;
    }

    public async Task StoreAsync(string key, string requestPath, int statusCode, string responseBody)
    {
        var now = DateTime.UtcNow;
        db.IdempotencyKeys.Add(new IdempotencyKey
        {
            Key = key,
            RequestPath = requestPath,
            StatusCode = statusCode,
            ResponseBody = responseBody,
            CreatedAt = now,
            ExpiresAt = now.Add(Ttl),
        });
        await db.SaveChangesAsync();
    }
}
