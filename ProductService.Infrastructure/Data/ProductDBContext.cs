using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;
using ProductService.Application.Interfaces;

namespace ProductService.Infrastructure.Data;

public class ProductDbContext : DbContext, IApplicationDbContext
{
    public ProductDbContext(
        DbContextOptions<ProductDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ProductDbContext).Assembly);
    }
}