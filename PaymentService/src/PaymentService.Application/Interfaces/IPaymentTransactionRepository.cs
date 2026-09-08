using PaymentService.Domain.Entities;

namespace PaymentService.Application.Interfaces;

public interface IPaymentTransactionRepository
{
    /// <summary>
    /// The core idempotency lookup. Called before doing anything else on
    /// every charge request.
    /// </summary>
    Task<PaymentTransaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default);

    Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(PaymentTransaction transaction, CancellationToken ct = default);

    /// <summary>
    /// Persists changes. Kept as an explicit step (rather than auto-saving
    /// inside Add/Update) so the application service controls exactly when
    /// a transaction is committed — important around the "insert Pending
    /// row BEFORE calling Stripe" ordering.
    /// </summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
