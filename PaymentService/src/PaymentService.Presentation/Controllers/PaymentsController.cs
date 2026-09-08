using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PaymentService.Application.DTOs;
using PaymentService.Application.Services;

namespace PaymentService.Presentation.Controllers;

[ApiController]
[Route("api/v1/payments")]
[Authorize] // requires a valid JWT, verified locally using Identity Service's signing key
public class PaymentsController : ControllerBase
{
    private readonly PaymentProcessingService _paymentProcessingService;

    public PaymentsController(PaymentProcessingService paymentProcessingService)
    {
        _paymentProcessingService = paymentProcessingService;
    }

  
    /// Called by Order Service during checkout. The Idempotency-Key header,
    /// not a body field, is what protects against duplicate charges on
    /// retry or timeout — see PaymentProcessingService for the full walkthrough.

    [HttpPost("charge")]
    [EnableRateLimiting("charge-endpoint")]
    [ProducesResponseType(typeof(ChargeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Charge(
        [FromBody] ChargeRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new
            {
                statusCode = 400,
                message = "The Idempotency-Key header is required.",
                errors = new[] { "Idempotency-Key header missing." }
            });
        }

        var result = await _paymentProcessingService.ChargeAsync(request, idempotencyKey, ct);
        return Ok(result);
    }

   
    /// The compensating action referenced in the Order State Machine design:
    /// used when a payment succeeded but its order could not be recovered.
    /// Not called automatically by anything yet in this build — invoked
    /// manually or by a future reconciliation job.

    [HttpPost("{transactionId:guid}/refund")]
    [ProducesResponseType(typeof(ChargeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Refund(Guid transactionId, CancellationToken ct)
    {
        var result = await _paymentProcessingService.RefundAsync(transactionId, ct);
        if (result is null)
            return NotFound(new { statusCode = 404, message = $"Transaction {transactionId} was not found." });

        return Ok(result);
    }
}
