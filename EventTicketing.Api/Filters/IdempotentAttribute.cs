using System.Text.Json;
using EventTicketing.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Filters;

/// <summary>
/// Action filter that provides idempotency for POST endpoints.
/// If the request carries an <c>Idempotency-Key</c> header the filter returns the
/// cached response on any repeat call, preventing duplicate side-effects (e.g.
/// double bookings). Keys expire after 24 hours.
///
/// If the header is absent the request is processed normally — idempotency is
/// opt-in for callers that need it.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    public const string HeaderName = "Idempotency-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var keyValues))
        {
            await next();
            return;
        }

        var key = keyValues.ToString();
        var path = context.HttpContext.Request.Path.Value ?? string.Empty;
        var service = context.HttpContext.RequestServices.GetRequiredService<IIdempotencyService>();

        // Return the cached response if one exists for this key + path.
        var cached = await service.GetAsync(key, path);
        if (cached is not null)
        {
            context.Result = new ContentResult
            {
                StatusCode = cached.StatusCode,
                Content = cached.ResponseBody,
                ContentType = "application/json",
            };
            return;
        }

        var executed = await next();

        // Store the response so future requests with the same key get the same result.
        // Ignore DbUpdateException: a concurrent request may have stored the same key
        // between our GetAsync check and this write — the response is still correct.
        if (executed.Exception is null && executed.Result is not null)
        {
            var (statusCode, body) = ExtractResult(executed.Result);
            try
            {
                await service.StoreAsync(key, path, statusCode, body);
            }
            catch (DbUpdateException) { }
        }
    }

    // Use camelCase to match ASP.NET Core's default JSON serialization policy.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static (int statusCode, string body) ExtractResult(IActionResult result) =>
        result switch
        {
            // ObjectResult covers CreatedAtActionResult, OkObjectResult,
            // ConflictObjectResult, NotFoundObjectResult, etc.
            ObjectResult obj => (obj.StatusCode ?? 200, JsonSerializer.Serialize(obj.Value, JsonOptions)),
            // StatusCodeResult covers NoContentResult, NotFoundResult, ConflictResult, etc.
            StatusCodeResult sc => (sc.StatusCode, string.Empty),
            _ => (200, string.Empty),
        };
}
