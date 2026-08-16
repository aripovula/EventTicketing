using EventTicketing.Api.Services;

namespace EventTicketing.Tests.Fakes;

public class FakePaymentService : IPaymentService
{
    public record ChargeCall(decimal Amount, string Currency, string PaymentMethodId, string IdempotencyKey);

    public List<ChargeCall> ChargeCalls { get; } = [];
    public List<string> RefundCalls { get; } = [];

    /// <summary>Set to true to make ChargeAsync throw a PaymentException.</summary>
    public bool ThrowOnCharge { get; set; }

    /// <summary>Overrides the PaymentIntentId returned on a successful charge.</summary>
    public string FakePaymentIntentId { get; set; } = "pi_fake_test";

    public Task<PaymentResult> ChargeAsync(
        decimal amount,
        string currency,
        string paymentMethodId,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        ChargeCalls.Add(new(amount, currency, paymentMethodId, idempotencyKey));

        if (ThrowOnCharge)
            throw new PaymentException("Your card was declined.");

        return Task.FromResult(new PaymentResult(FakePaymentIntentId));
    }

    public Task RefundAsync(string paymentIntentId, CancellationToken ct = default)
    {
        RefundCalls.Add(paymentIntentId);
        return Task.CompletedTask;
    }
}
