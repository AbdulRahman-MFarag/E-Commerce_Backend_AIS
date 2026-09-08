namespace PaymentService.Domain.Enums;

/// <summary>
/// Deliberately separate from Order Service's OrderStatus. A payment can be
/// Succeeded while its order is still Pending or stuck — merging the two
/// state machines into one would make that (real, important) window invisible.
/// </summary>
public enum PaymentStatus
{
    Pending,
    Succeeded,
    Failed,
    Refunded
}
