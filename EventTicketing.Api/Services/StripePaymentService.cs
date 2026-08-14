using Stripe;

namespace EventTicketing.Api.Services;

/// <summary>
/// Stripe implementation of <see cref="IPaymentService"/> using the PaymentIntents API.
/// Configured via <c>Stripe:SecretKey</c> in appsettings / environment variables.
/// </summary>
public class StripePaymentService(
    PaymentIntentService paymentIntentService,
    RefundService refundService,
    ILogger<StripePaymentService> logger) : IPaymentService
{
    public async Task<PaymentResult> ChargeAsync(
        decimal amount,
        string currency,
        string paymentMethodId,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        // Stripe amounts are in the currency's smallest unit (cents for USD).
        var amountInCents = (long)(amount * 100);

        var options = new PaymentIntentCreateOptions
        {
            Amount = amountInCents,
            Currency = currency,
            PaymentMethod = paymentMethodId,
            // confirm: true creates and captures the payment in one call.
            Confirm = true,
            // off_session: true signals this is a server-side confirmation
            // (no 3D Secure redirect required for test cards).
            OffSession = true,
        };

        var requestOptions = new RequestOptions
        {
            // Stripe deduplicates on this key for 24 hours, matching the TTL
            // of the existing IdempotencyKey table entry for the same booking.
            IdempotencyKey = idempotencyKey,
        };

        try
        {
            var intent = await paymentIntentService.CreateAsync(options, requestOptions, ct);

            if (intent.Status != "succeeded")
                throw new PaymentException($"Payment did not succeed (status: {intent.Status}).");

            logger.LogInformation(
                "Stripe PaymentIntent {PaymentIntentId} succeeded for {Amount} {Currency}",
                intent.Id, amount, currency.ToUpperInvariant());

            return new PaymentResult(intent.Id);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(
                "Stripe charge failed: {StripeCode} — {Message}",
                ex.StripeError?.Code, ex.Message);

            throw new PaymentException(ex.StripeError?.Message ?? ex.Message);
        }
    }

    public async Task RefundAsync(string paymentIntentId, CancellationToken ct = default)
    {
        try
        {
            var options = new RefundCreateOptions { PaymentIntent = paymentIntentId };
            await refundService.CreateAsync(options, cancellationToken: ct);

            logger.LogInformation(
                "Refunded Stripe PaymentIntent {PaymentIntentId} after DB save failure",
                paymentIntentId);
        }
        catch (StripeException ex)
        {
            // Log but do not rethrow — the caller is already handling a DB error.
            // A failed refund must be surfaced via alerting / manual reconciliation.
            logger.LogError(ex,
                "Failed to refund Stripe PaymentIntent {PaymentIntentId}. Manual reconciliation required.",
                paymentIntentId);
        }
    }
}
