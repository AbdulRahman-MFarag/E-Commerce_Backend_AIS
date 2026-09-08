using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;

namespace ProductService.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Product> Products { get; }
        DbSet<Category> Categories { get; }
        DbSet<ProductImage> ProductImages { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}