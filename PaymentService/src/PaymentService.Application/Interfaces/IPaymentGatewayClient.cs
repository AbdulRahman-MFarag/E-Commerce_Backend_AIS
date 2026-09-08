namespace PaymentService.Application.Interfaces;

/// <summary>
/// Abstracts the actual payment provider away from the rest of the service.
/// If Stripe were ever swapped for another provider, only the Infrastructure-layer
/// implementation of this interface changes — nothing in the Application layer,
/// and nothing in Order Service, needs to know or care.
/// </summary>
public interface IPaymentGatewayClient
{
    Task<GatewayChargeResult> ChargeAsync(string paymentMethodToken, decimal amount, string currency, CancellationToken ct = default);
}

public class GatewayChargeResult
{
    public bool Success { get; init; }
    public string? ProviderTransactionId { get; init; }
    public string? FailureReason { get; init; }
}
