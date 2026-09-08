namespace Order.Api.Domain;

public enum OrderStatus
{
    PendingPayment = 1,
    Paid = 2,
    Confirmed = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6,
    PaymentFailed = 7
}
