using System.ComponentModel.DataAnnotations;

namespace PaymentService.Application.DTOs;

/// What Order Service sends us. Note: no card number here, ever.
/// PaymentMethodToken comes from Stripe.js/Stripe SDK running on the
/// frontend, which means raw card data never reaches our backend at all.

public class ChargeRequest
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "usd";

    [Required]
    public string PaymentMethodToken { get; set; } = default!;

    // Deliberately NOT read from the request body. The idempotency key
    // travels as an "Idempotency-Key" HTTP header instead (see
    // PaymentsController), matching Stripe's own convention that the key
    // describes the whole request, not just another data field.
}
