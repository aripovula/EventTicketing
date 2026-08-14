namespace EventTicketing.Api.Services;

/// <summary>
/// Abstracts the payment provider so the booking flow is testable without
/// real network calls. The concrete implementation uses the Stripe PaymentIntents API.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Creates and confirms a PaymentIntent for the given amount.
    /// </summary>
    /// <param name="amount">Charge amount in the currency's smallest unit (e.g. cents for USD).</param>
    /// <param name="currency">ISO 4217 currency code (e.g. "usd").</param>
    /// <param name="paymentMethodId">Stripe PaymentMethod ID supplied by the client (e.g. "pm_card_visa").</param>
    /// <param name="idempotencyKey">
    /// Passed to Stripe as its own idempotency key. Stripe deduplicates on this value for 24 hours,
    /// which pairs with the existing <c>Idempotency-Key</c> header to prevent double charges.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="PaymentResult"/> containing the PaymentIntent ID to store on the order.</returns>
    /// <exception cref="PaymentException">Thrown when Stripe declines the charge or returns an error.</exception>
    Task<PaymentResult> ChargeAsync(
        decimal amount,
        string currency,
        string paymentMethodId,
        string idempotencyKey,
        CancellationToken ct = default);

    /// <summary>
    /// Refunds a previously confirmed PaymentIntent. Called as a compensation step
    /// when the database write fails after a successful charge.
    /// </summary>
    /// <param name="paymentIntentId">The Stripe PaymentIntent ID to refund.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RefundAsync(string paymentIntentId, CancellationToken ct = default);
}

/// <summary>Successful charge result.</summary>
/// <param name="PaymentIntentId">The Stripe PaymentIntent ID — stored on the <c>Order</c> for reference.</param>
public record PaymentResult(string PaymentIntentId);

/// <summary>
/// Thrown by <see cref="IPaymentService"/> when the payment provider rejects the charge.
/// The exception message is safe to surface to the caller as a 402 response body.
/// </summary>
public class PaymentException(string message) : Exception(message);
