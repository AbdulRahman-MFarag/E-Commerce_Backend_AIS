using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Order.Api.Data;
using OrderEntity = Order.Api.Domain.Order;
using OrderStatus = Order.Api.Domain.OrderStatus;
using OrderItemEntity = Order.Api.Domain.OrderItem;
using Order.Api.DTOs;

namespace Order.Api.Services;

public class OrderService : IOrderService
{
    private readonly OrderDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ICatalogService _catalogService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        OrderDbContext db,
        IMemoryCache cache,
        ICatalogService catalogService,
        ILogger<OrderService> logger)
    {
        _db = db;
        _cache = cache;
        _catalogService = catalogService;
        _logger = logger;
    }

    public async Task<OrderResponse> CreateAsync(
        string userId,
        string idempotencyKey,
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency-Key header is required.");

        var existing = await _db.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.IdempotencyKey == idempotencyKey && x.UserId == userId,
                cancellationToken);

        if (existing is not null)
            return Map(existing);

        var order = new OrderEntity
        {
            UserId = userId,
            IdempotencyKey = idempotencyKey.Trim(),
            Status = OrderStatus.PendingPayment
        };

        string? orderCurrency = null;

        foreach (var item in request.Items)
        {
            // Price, name and currency always come from Catalog, never from the client -
            // otherwise anyone could submit their own price for checkout.
            var product = await _catalogService.GetProductAsync(item.ProductId, cancellationToken);

            if (product is null)
                throw new ArgumentException($"Product {item.ProductId} does not exist.");

            if (orderCurrency is null)
                orderCurrency = product.Currency;
            else if (orderCurrency != product.Currency)
                throw new ArgumentException(
                    "All items in an order must use the same currency.");

            order.Items.Add(new OrderItemEntity
            {
                ProductId = product.ProductId,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = item.Quantity
            });
        }

        order.Currency = orderCurrency ?? "EGP";
        order.TotalAmount = order.Items.Sum(x => x.LineTotal);

        _db.Orders.Add(order);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another identical request may have won the race.
            var createdByAnotherRequest = await _db.Orders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(
                    x => x.IdempotencyKey == idempotencyKey && x.UserId == userId,
                    cancellationToken);

            if (createdByAnotherRequest is not null)
                return Map(createdByAnotherRequest);

            throw;
        }

        _logger.LogInformation(
            "Order {OrderId} created for user {UserId} with status {Status}",
            order.Id,
            userId,
            order.Status);

        return Map(order);
    }

    public async Task<IReadOnlyList<OrderResponse>> GetMyOrdersAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"orders:user:{userId}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<OrderResponse>? cached) &&
            cached is not null)
        {
            return cached;
        }

        var orders = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var result = orders.Select(Map).ToList();

        _cache.Set(cacheKey, result, TimeSpan.FromSeconds(30));

        return result;
    }

    public async Task<OrderResponse?> GetByIdAsync(
        Guid orderId,
        string userId,
        CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.Id == orderId && x.UserId == userId,
                cancellationToken);

        return order is null ? null : Map(order);
    }

    public async Task<OrderResponse?> UpdateStatusAsync(
        Guid orderId,
        string userId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.Id == orderId && x.UserId == userId,
                cancellationToken);

        if (order is null)
            return null;

        if (!IsValidTransition(order.Status, request.Status))
            throw new InvalidOperationException(
                $"Invalid order status transition: {order.Status} -> {request.Status}");

        order.Status = request.Status;
        order.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _cache.Remove($"orders:user:{userId}");

        _logger.LogInformation(
            "Order {OrderId} status changed to {Status}",
            order.Id,
            order.Status);

        return Map(order);
    }

    private static bool IsValidTransition(OrderStatus current, OrderStatus next)
    {
        return current switch
        {
            OrderStatus.PendingPayment =>
                next is OrderStatus.Paid or OrderStatus.PaymentFailed or OrderStatus.Cancelled,

            OrderStatus.Paid =>
                next is OrderStatus.Confirmed or OrderStatus.Cancelled,

            OrderStatus.Confirmed =>
                next is OrderStatus.Shipped or OrderStatus.Cancelled,

            OrderStatus.Shipped =>
                next is OrderStatus.Delivered,

            OrderStatus.PaymentFailed =>
                next is OrderStatus.Cancelled,

            OrderStatus.Delivered => false,
            OrderStatus.Cancelled => false,
            _ => false
        };
    }

    private static OrderResponse Map(OrderEntity order)
    {
        return new OrderResponse(
            order.Id,
            order.UserId,
            order.Status,
            order.TotalAmount,
            order.Currency,
            order.CreatedAtUtc,
            order.Items
                .Select(x => new OrderItemResponse(
                    x.ProductId,
                    x.ProductName,
                    x.UnitPrice,
                    x.Quantity,
                    x.LineTotal))
                .ToList());
    }
}
