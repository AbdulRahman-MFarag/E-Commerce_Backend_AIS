using Microsoft.EntityFrameworkCore;
using PaymentService.Domain.Entities;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentDbContext : DbContext
{
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.ToTable("PaymentTransactions");
            entity.HasKey(t => t.Id);

            // This is the actual database-level guarantee behind the whole
            // idempotency design. Even if the application-level check in
            // PaymentProcessingService somehow raced (two requests both
            // passing the "does this exist" check at the same instant),
            // this unique index makes the database itself reject the
            // second insert.
            entity.HasIndex(t => t.IdempotencyKey).IsUnique();

            // OrderId is intentionally just a column — no HasOne/WithMany,
            // no foreign key configuration, because there is no local
            // Order table to relate it to. This is the cross-service
            // "reference by value" pattern made concrete in the schema.
            entity.Property(t => t.OrderId).IsRequired();

            entity.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            entity.Property(t => t.Currency).HasMaxLength(3).IsRequired();
            entity.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(t => t.Provider).HasMaxLength(50);
            entity.Property(t => t.ProviderTransactionId).HasMaxLength(255);
            entity.Property(t => t.IdempotencyKey).HasMaxLength(255).IsRequired();
        });
    }
}
