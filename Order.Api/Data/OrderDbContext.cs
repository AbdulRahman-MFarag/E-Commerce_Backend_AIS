using Microsoft.EntityFrameworkCore;
using OrderEntity = Order.Api.Domain.Order;
using OrderItemEntity = Order.Api.Domain.OrderItem;

namespace Order.Api.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<OrderItemEntity> OrderItems => Set<OrderItemEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderEntity>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.UserId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.TotalAmount)
                .HasPrecision(18, 2);

            entity.Property(x => x.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(x => x.IdempotencyKey)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(x => new { x.UserId, x.CreatedAtUtc });

            entity.HasIndex(x => new { x.UserId, x.IdempotencyKey }).IsUnique();

            entity.HasMany(x => x.Items)
                .WithOne(x => x.Order)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItemEntity>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ProductName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(x => x.Quantity)
                .IsRequired();
        });
    }
}
