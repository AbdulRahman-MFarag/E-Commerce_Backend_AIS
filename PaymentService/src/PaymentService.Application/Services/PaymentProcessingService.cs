using Microsoft.Extensions.Logging;
using PaymentService.Application.DTOs;
using PaymentService.Application.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Services;


/// This class is the single most important file in Payment Service.
/// It implements, step by step, the idempotency walkthrough:
///
///   1. Check if this idempotency key has already been used.
///      -> If yes, return the STORED result. Never call Stripe again.
///   2. If not, insert a Pending row FIRST, before calling Stripe at all.
///      -> This ordering matters: if the process crashes between inserting
///         the row and calling Stripe, there is no ambiguity — the row is
///         Pending, meaning "we don't yet know if this succeeded," and a
///         reconciliation process can safely retry or investigate it.
///         If we called Stripe first and only recorded the key afterward,
///         a crash in that gap would mean a real charge exists with no
///         idempotency record at all — exactly the failure this whole
///         design exists to prevent.
///   3. Call the payment gateway (Stripe, via IPaymentGatewayClient).
///   4. Update the row to Succeeded or Failed based on the real result.
public class PaymentProcessingService
{
    private readonly IPaymentTransactionRepository _repository;
    private readonly IPaymentGatewayClient _gatewayClient;
    private readonly ILogger<PaymentProcessingService> _logger;

    public PaymentProcessingService(
        IPaymentTransactionRepository repository,
        IPaymentGatewayClient gatewayClient,
        ILogger<PaymentProcessingService> logger)
    {
        _repository = repository;
        _gatewayClient = gatewayClient;
        _logger = logger;
    }

    public async Task<ChargeResponse> ChargeAsync(ChargeRequest request, string idempotencyKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("An Idempotency-Key header is required for all charge requests.", nameof(idempotencyKey));

        //Step 1: idempotency check
        var existing = await _repository.GetByIdempotencyKeyAsync(idempotencyKey, ct);
        if (existing is not null)
        {
            _logger.LogInformation(
                "Idempotency key {IdempotencyKey} already processed as transaction {TransactionId} with status {Status}. Returning stored result, Stripe was NOT called again.",
                idempotencyKey, existing.Id, existing.Status);

            return ToResponse(existing);
        }

        // Step 2: insert Pending row BEFORE contacting Stripe
        var transaction = PaymentTransaction.CreatePending(
            request.OrderId, idempotencyKey, request.Amount, request.Currency);

        await _repository.AddAsync(transaction, ct);
        await _repository.SaveChangesAsync(ct);

        // Step 3: call the gateway 
        GatewayChargeResult gatewayResult;
        try
        {
            gatewayResult = await _gatewayClient.ChargeAsync(
                request.PaymentMethodToken, request.Amount, request.Currency, ct);
        }
        catch (Exception ex)
        {
            // A network/provider-level exception is treated as a failed
            // charge, not silently swallowed — the row must always end up
            // in a terminal, honest state.
            _logger.LogError(ex, "Payment gateway call threw an exception for order {OrderId}.", request.OrderId);
            transaction.MarkFailed($"Gateway error: {ex.Message}");
            await _repository.SaveChangesAsync(ct);
            return ToResponse(transaction);
        }

        //Step 4: update the row based on the real result
        if (gatewayResult.Success)
        {
            transaction.MarkSucceeded(gatewayResult.ProviderTransactionId!);
            _logger.LogInformation("Charge succeeded for order {OrderId}, transaction {TransactionId}.", request.OrderId, transaction.Id);
        }
        else
        {
            transaction.MarkFailed(gatewayResult.FailureReason ?? "Unknown failure");
            _logger.LogWarning("Charge failed for order {OrderId}, transaction {TransactionId}: {Reason}", request.OrderId, transaction.Id, gatewayResult.FailureReason);
        }

        await _repository.SaveChangesAsync(ct);
        return ToResponse(transaction);
    }

    public async Task<ChargeResponse?> RefundAsync(Guid transactionId, CancellationToken ct = default)
    {
        var transaction = await _repository.GetByIdAsync(transactionId, ct);
        if (transaction is null)
            return null;

        // Compensating action referenced in the Order State Machine section
        // of the system design: used when payment succeeded but the order
        // could not be finalized and cannot be recovered.
        transaction.MarkRefunded();
        await _repository.SaveChangesAsync(ct);
        return ToResponse(transaction);
    }

    private static ChargeResponse ToResponse(PaymentTransaction t) => new()
    {
        TransactionId = t.Id,
        Status = t.Status.ToString(),
        ProviderTransactionId = t.ProviderTransactionId,
        FailureReason = t.FailureReason
    };
}
