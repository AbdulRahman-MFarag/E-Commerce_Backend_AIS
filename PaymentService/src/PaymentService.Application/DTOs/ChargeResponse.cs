namespace PaymentService.Application.DTOs;

/// <summary>
/// What we hand back to Order Service. TransactionId is the value Order
/// Service stores in its own Order.PaymentReference column — the reverse
/// pointer described in the ERD document.
/// </summary>
public class ChargeResponse
{
    public Guid TransactionId { get; set; }
    public string Status { get; set; } = default!;
    public string? ProviderTransactionId { get; set; }
    public string? FailureReason { get; set; }
}
