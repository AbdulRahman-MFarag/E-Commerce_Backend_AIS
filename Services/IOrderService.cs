using Order.Api.DTOs;

namespace Order.Api.Services;

public interface IOrderService
{
    Task<OrderResponse> CreateAsync(
        string userId,
        string idempotencyKey,
        CreateOrderRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderResponse>> GetMyOrdersAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<OrderResponse?> GetByIdAsync(
        Guid orderId,
        string userId,
        CancellationToken cancellationToken);

    Task<OrderResponse?> UpdateStatusAsync(
        Guid orderId,
        string userId,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken);
}
