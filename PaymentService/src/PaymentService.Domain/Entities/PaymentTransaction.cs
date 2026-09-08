using PaymentService.Domain.Enums;

namespace PaymentService.Domain.Entities;

/// <summary>
/// The single entity owned by Payment Service. Note there is no navigation
/// property to an "Order" here — OrderId is a plain value copied from
/// Order Service via the API request, not a database-enforced foreign key.
/// Payment Service has no Orders table to relate it to.
/// </summary>
public class PaymentTransaction
{
    public Guid Id { get; private set; }

    // Plain reference value — points at Order.Id in Order Service's own
    // database. Nothing here validates that the order actually exists;
    // that trust comes from the caller (Order Service) having already
    // confirmed it before calling us.
    public Guid OrderId { get; private set; }

    // The idempotency key is what actually prevents a double charge.
    // A unique index on this column (configured in the EF Core mapping)
    // is the real enforcement mechanism — this property alone is not enough.
    public string IdempotencyKey { get; private set; } = default!;

    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "usd";

    public PaymentStatus Status { get; private set; }

    public string Provider { get; private set; } = "Stripe";
    public string? ProviderTransactionId { get; private set; }
    public string? FailureReason { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // EF Core needs a parameterless constructor; kept private so it can't
    // be misused to create an entity in an invalid state from application code.
    private PaymentTransaction() { }

    /// <summary>
    /// Creates a new transaction in the Pending state. This is called
    /// BEFORE the provider (Stripe) is ever contacted — see the walkthrough
    /// in PaymentProcessingService for why that ordering matters.
    /// </summary>
    public static PaymentTransaction CreatePending(Guid orderId, string idempotencyKey, decimal amount, string currency)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Charge amount must be greater than zero.");

        var now = DateTime.UtcNow;
        return new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            IdempotencyKey = idempotencyKey,
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkSucceeded(string providerTransactionId)
    {
        Status = PaymentStatus.Succeeded;
        ProviderTransactionId = providerTransactionId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = PaymentStatus.Failed;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkRefunded()
    {
        if (Status != PaymentStatus.Succeeded)
            throw new InvalidOperationException("Only a Succeeded payment can be refunded.");

        Status = PaymentStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
    }
}
