using EventTicketing.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Stripe;

namespace EventTicketing.Tests.Services;

/// <summary>
/// Unit tests for StripePaymentService. Uses NSubstitute to mock PaymentIntentService
/// and RefundService so no network calls or Stripe API key are needed.
/// </summary>
public class StripePaymentServiceTests
{
    private readonly PaymentIntentService _intentService;
    private readonly RefundService _refundService;
    private readonly StripePaymentService _sut;

    public StripePaymentServiceTests()
    {
        _intentService = Substitute.For<PaymentIntentService>();
        _refundService = Substitute.For<RefundService>();
        _sut = new StripePaymentService(
            _intentService,
            _refundService,
            NullLogger<StripePaymentService>.Instance);
    }

    // ── ChargeAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ChargeAsync_SucceededIntent_ReturnsPaymentResultWithIntentId()
    {
        var intent = new PaymentIntent { Id = "pi_test_123", Status = "succeeded" };
        _intentService.CreateAsync(Arg.Any<PaymentIntentCreateOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(intent);

        var result = await _sut.ChargeAsync(10m, "usd", "pm_card_visa", "idem-key");

        Assert.Equal("pi_test_123", result.PaymentIntentId);
    }

    [Fact]
    public async Task ChargeAsync_ConvertsDollarsToCents()
    {
        var intent = new PaymentIntent { Id = "pi_cents", Status = "succeeded" };
        _intentService.CreateAsync(Arg.Any<PaymentIntentCreateOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(intent);

        await _sut.ChargeAsync(12.50m, "usd", "pm_card_visa", "key");

        await _intentService.Received(1).CreateAsync(
            Arg.Is<PaymentIntentCreateOptions>(o => o.Amount == 1250),
            Arg.Any<RequestOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChargeAsync_PassesCurrencyToStripe()
    {
        var intent = new PaymentIntent { Id = "pi_cur", Status = "succeeded" };
        _intentService.CreateAsync(Arg.Any<PaymentIntentCreateOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(intent);

        await _sut.ChargeAsync(5m, "eur", "pm_card_visa", "key");

        await _intentService.Received(1).CreateAsync(
            Arg.Is<PaymentIntentCreateOptions>(o => o.Currency == "eur"),
            Arg.Any<RequestOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChargeAsync_SetsConfirmAndOffSession()
    {
        var intent = new PaymentIntent { Id = "pi_confirm", Status = "succeeded" };
        _intentService.CreateAsync(Arg.Any<PaymentIntentCreateOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(intent);

        await _sut.ChargeAsync(5m, "usd", "pm_card_visa", "key");

        await _intentService.Received(1).CreateAsync(
            Arg.Is<PaymentIntentCreateOptions>(o => o.Confirm == true && o.OffSession == true),
            Arg.Any<RequestOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChargeAsync_ForwardsIdempotencyKeyToRequestOptions()
    {
        var intent = new PaymentIntent { Id = "pi_idem", Status = "succeeded" };
        _intentService.CreateAsync(Arg.Any<PaymentIntentCreateOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(intent);

        await _sut.ChargeAsync(5m, "usd", "pm_card_visa", "my-idempotency-key");

        await _intentService.Received(1).CreateAsync(
            Arg.Any<PaymentIntentCreateOptions>(),
            Arg.Is<RequestOptions>(o => o.IdempotencyKey == "my-idempotency-key"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChargeAsync_NonSucceededStatus_ThrowsPaymentException()
    {
        var intent = new PaymentIntent { Id = "pi_pend", Status = "requires_action" };
        _intentService.CreateAsync(Arg.Any<PaymentIntentCreateOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(intent);

        await Assert.ThrowsAsync<PaymentException>(() =>
            _sut.ChargeAsync(5m, "usd", "pm_card_visa", "key"));
    }

    [Fact]
    public async Task ChargeAsync_StripeException_ThrowsPaymentException()
    {
        _intentService.CreateAsync(Arg.Any<PaymentIntentCreateOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new StripeException("card_declined"));

        await Assert.ThrowsAsync<PaymentException>(() =>
            _sut.ChargeAsync(5m, "usd", "pm_card_declined", "key"));
    }

    [Fact]
    public async Task ChargeAsync_StripeException_WrapsOriginalMessage()
    {
        var stripeEx = new StripeException("Your card was declined.")
        {
            StripeError = new StripeError { Message = "Your card was declined." }
        };
        _intentService.CreateAsync(Arg.Any<PaymentIntentCreateOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(stripeEx);

        var ex = await Assert.ThrowsAsync<PaymentException>(() =>
            _sut.ChargeAsync(5m, "usd", "pm_card_declined", "key"));

        Assert.Contains("declined", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── RefundAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RefundAsync_PassesPaymentIntentIdToStripe()
    {
        _refundService.CreateAsync(Arg.Any<RefundCreateOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .Returns(new Refund());

        await _sut.RefundAsync("pi_to_refund");

        await _refundService.Received(1).CreateAsync(
            Arg.Is<RefundCreateOptions>(o => o.PaymentIntent == "pi_to_refund"),
            Arg.Any<RequestOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefundAsync_StripeException_DoesNotRethrow()
    {
        _refundService.CreateAsync(Arg.Any<RefundCreateOptions>(), Arg.Any<RequestOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new StripeException("refund_failed"));

        // Must complete without throwing — caller is already handling a DB error
        await _sut.RefundAsync("pi_bad");
    }
}
