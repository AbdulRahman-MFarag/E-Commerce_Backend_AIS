using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Interfaces;
using PaymentService.Domain.Entities;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly PaymentDbContext _context;

    public PaymentTransactionRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public Task<PaymentTransaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default) =>
        _context.PaymentTransactions.FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, ct);

    public Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.PaymentTransactions.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddAsync(PaymentTransaction transaction, CancellationToken ct = default) =>
        await _context.PaymentTransactions.AddAsync(transaction, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
