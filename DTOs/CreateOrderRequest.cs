using System.ComponentModel.DataAnnotations;

namespace Order.Api.DTOs;

public class CreateOrderRequest
{
    [Required]
    [MinLength(1)]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
    public Guid ProductId { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; }
}
