namespace Order.Api.Services;

public record CatalogProduct(Guid ProductId, string Name, decimal Price, string Currency);

public interface ICatalogService
{
    Task<CatalogProduct?> GetProductAsync(Guid productId, CancellationToken cancellationToken);
}
