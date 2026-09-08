namespace Order.Api.Domain;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public decimal TotalAmount { get; set; }

    // ISO 4217 code (e.g. "EGP"). Snapshotted from Catalog at checkout time,
    // same reasoning as OrderItem.UnitPrice.
    public string Currency { get; set; } = "EGP";

    // Used to make repeated checkout requests safe.
    public string IdempotencyKey { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = new();
}
