namespace Order.Api.Services;

// Stand-in for the real Catalog service.
// Swap this for an HttpClient-based implementation once Catalog exposes an internal
// endpoint (e.g. GET /internal/products/{id}) - the ICatalogService contract stays the same.
public class InMemoryCatalogService : ICatalogService
{
    private static readonly Dictionary<Guid, CatalogProduct> Products = new()
    {
        [Guid.Parse("11111111-1111-1111-1111-111111111111")] =
            new CatalogProduct(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Laptop", 25000m, "EGP"),
        [Guid.Parse("22222222-2222-2222-2222-222222222222")] =
            new CatalogProduct(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Wireless Mouse", 350m, "EGP"),
    };

    public Task<CatalogProduct?> GetProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        Products.TryGetValue(productId, out var product);
        return Task.FromResult(product);
    }
}
