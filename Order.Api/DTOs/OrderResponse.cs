using Order.Api.Domain;

namespace Order.Api.DTOs;

public record OrderResponse(
    Guid Id,
    string UserId,
    OrderStatus Status,
    decimal Amount,
    string Currency,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemResponse> Items);

public record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public record UpdateOrderStatusRequest(OrderStatus Status);
