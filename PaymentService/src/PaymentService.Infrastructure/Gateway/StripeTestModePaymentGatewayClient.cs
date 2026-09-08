using PaymentService.Application.Interfaces;

namespace PaymentService.Infrastructure.Gateway;

/// <summary>
/// Stripe TEST MODE client. Real payment integration is explicitly out of
/// scope for this project (per the requirements document) — this class
/// simulates Stripe's well-known test-card behavior so the rest of the
/// system (idempotency, state machine, compensation) can be built and
/// tested against realistic success/failure outcomes without needing a
/// live Stripe account or network access.
///
/// To swap in the REAL Stripe SDK later, this is the only file that
/// changes — everything else in the system depends on IPaymentGatewayClient,
/// not on Stripe directly.
/// </summary>
public class StripeTestModePaymentGatewayClient : IPaymentGatewayClient
{
    // Stripe's real test mode uses specific fake card numbers to trigger
    // specific outcomes (e.g. 4242... always succeeds, 4000000000000002
    // always declines). We mirror that convention here using the token
    // string, since we never see a real card number.
    private const string DeclineToken = "tok_chargeDeclined";

    public Task<GatewayChargeResult> ChargeAsync(string paymentMethodToken, decimal amount, string currency, CancellationToken ct = default)
    {
        // Simulate network latency so the rest of the system is built
        // against realistic timing, not an instant in-process call.
        return SimulateAsync(paymentMethodToken);
    }

    private static async Task<GatewayChargeResult> SimulateAsync(string token)
    {
        await Task.Delay(150);

        if (string.IsNullOrWhiteSpace(token))
        {
            return new GatewayChargeResult { Success = false, FailureReason = "Missing payment method token." };
        }

        if (token == DeclineToken)
        {
            return new GatewayChargeResult { Success = false, FailureReason = "Your card was declined." };
        }

        return new GatewayChargeResult
        {
            Success = true,
            ProviderTransactionId = $"ch_test_{Guid.NewGuid():N}"
        };
    }
}
