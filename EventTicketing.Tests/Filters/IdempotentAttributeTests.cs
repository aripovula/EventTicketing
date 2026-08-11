using EventTicketing.Api.Filters;
using EventTicketing.Api.Models;
using EventTicketing.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace EventTicketing.Tests.Filters;

public class IdempotentAttributeTests
{
    private readonly IIdempotencyService _service;
    private readonly IdempotentAttribute _filter;

    private const string TestKey = "test-idempotency-key";
    private const string TestPath = "/api/events/1/book";

    public IdempotentAttributeTests()
    {
        _service = Substitute.For<IIdempotencyService>();
        _filter = new IdempotentAttribute();
    }

    private (ActionExecutingContext executingContext, ActionExecutionDelegate next, ActionExecutedContext executedContext) MakeContext(
        string? idempotencyKey = null,
        string path = TestPath,
        IActionResult? actionResult = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_service);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        httpContext.Request.Path = path;
        if (idempotencyKey is not null)
            httpContext.Request.Headers[IdempotentAttribute.HeaderName] = idempotencyKey;

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        var executingContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller: new object());

        var executedContext = new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            controller: new object())
        {
            Result = actionResult ?? new ObjectResult(new { id = 1 }) { StatusCode = 201 }
        };

        ActionExecutionDelegate next = () => Task.FromResult(executedContext);
        return (executingContext, next, executedContext);
    }

    // ── No header ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task NoHeader_DoesNotCheckService()
    {
        var (ctx, next, _) = MakeContext(idempotencyKey: null);

        await _filter.OnActionExecutionAsync(ctx, next);

        await _service.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task NoHeader_DoesNotStoreResponse()
    {
        var (ctx, next, _) = MakeContext(idempotencyKey: null);

        await _filter.OnActionExecutionAsync(ctx, next);

        await _service.DidNotReceive().StoreAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>());
    }

    // ── Cache hit ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CacheHit_ReturnsCachedStatusCode()
    {
        _service.GetAsync(TestKey, TestPath).Returns(new IdempotencyKey
        {
            StatusCode = 201,
            ResponseBody = "{\"id\":1}"
        });
        var (ctx, next, _) = MakeContext(TestKey);

        await _filter.OnActionExecutionAsync(ctx, next);

        var result = Assert.IsType<ContentResult>(ctx.Result);
        Assert.Equal(201, result.StatusCode);
    }

    [Fact]
    public async Task CacheHit_ReturnsCachedBody()
    {
        _service.GetAsync(TestKey, TestPath).Returns(new IdempotencyKey
        {
            StatusCode = 201,
            ResponseBody = "{\"id\":42}"
        });
        var (ctx, next, _) = MakeContext(TestKey);

        await _filter.OnActionExecutionAsync(ctx, next);

        var result = Assert.IsType<ContentResult>(ctx.Result);
        Assert.Equal("{\"id\":42}", result.Content);
    }

    [Fact]
    public async Task CacheHit_DoesNotCallNext()
    {
        _service.GetAsync(TestKey, TestPath).Returns(new IdempotencyKey
        {
            StatusCode = 201,
            ResponseBody = "{}"
        });
        var nextCalled = false;
        var (ctx, _, executedCtx) = MakeContext(TestKey);
        ActionExecutionDelegate next = () => { nextCalled = true; return Task.FromResult(executedCtx); };

        await _filter.OnActionExecutionAsync(ctx, next);

        Assert.False(nextCalled);
    }

    // ── Cache miss ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CacheMiss_StoresResponseWithCorrectStatusCode()
    {
        _service.GetAsync(TestKey, TestPath).Returns((IdempotencyKey?)null);
        var (ctx, next, _) = MakeContext(TestKey,
            actionResult: new ObjectResult(new { id = 5 }) { StatusCode = 201 });

        await _filter.OnActionExecutionAsync(ctx, next);

        await _service.Received(1).StoreAsync(TestKey, TestPath, 201, Arg.Any<string>());
    }

    [Fact]
    public async Task CacheMiss_StoresResponseForNonSuccessStatus()
    {
        _service.GetAsync(TestKey, TestPath).Returns((IdempotencyKey?)null);
        var (ctx, next, _) = MakeContext(TestKey,
            actionResult: new ConflictResult());

        await _filter.OnActionExecutionAsync(ctx, next);

        await _service.Received(1).StoreAsync(TestKey, TestPath, 409, Arg.Any<string>());
    }

    // ── Race condition ────────────────────────────────────────────────────────

    [Fact]
    public async Task DbUpdateException_IsSwallowed()
    {
        _service.GetAsync(TestKey, TestPath).Returns((IdempotencyKey?)null);
        _service.StoreAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>())
            .ThrowsAsync(new DbUpdateException());
        var (ctx, next, _) = MakeContext(TestKey);

        // Should not throw
        await _filter.OnActionExecutionAsync(ctx, next);
    }
}
