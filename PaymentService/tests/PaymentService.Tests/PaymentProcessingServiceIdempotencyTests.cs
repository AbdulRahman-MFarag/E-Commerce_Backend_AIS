using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PaymentService.Application.DTOs;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Services;
using PaymentService.Domain.Enums;
using PaymentService.Infrastructure.Persistence;
using Xunit;

namespace PaymentService.Tests;

/// <summary>
/// This is the test called out specifically in the requirements document:
/// "how do you test the concurrency and idempotency paths specifically."
///
/// The key assertion is NOT just "only one row exists in the database" —
/// that alone wouldn't prove the customer wasn't charged twice, only that
/// our own bookkeeping is tidy. The real proof is that the payment
/// gateway (Stripe, mocked here) was called EXACTLY ONCE, even though the
/// charge endpoint was invoked twice with the same idempotency key.
/// </summary>
public class PaymentProcessingServiceIdempotencyTests
{
    private static PaymentDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PaymentDbContext(options);
    }

    [Fact]
    public async Task ChargeAsync_CalledTwiceWithSameIdempotencyKey_OnlyCallsGatewayOnce()
    {
        // Arrange
        await using var dbContext = CreateInMemoryContext();
        var repository = new PaymentTransactionRepository(dbContext);

        var gatewayMock = new Mock<IPaymentGatewayClient>();
        gatewayMock
            .Setup(g => g.ChargeAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayChargeResult { Success = true, ProviderTransactionId = "ch_test_123" });

        var service = new PaymentProcessingService(repository, gatewayMock.Object, NullLogger<PaymentProcessingService>.Instance);

        var request = new ChargeRequest
        {
            OrderId = Guid.NewGuid(),
            Amount = 49.99m,
            Currency = "usd",
            PaymentMethodToken = "tok_visa"
        };
        const string idempotencyKey = "retry-test-key-001";

        // Act: simulate the frontend retrying the same checkout request
        // after a network timeout, sending the identical idempotency key.
        var firstResult = await service.ChargeAsync(request, idempotencyKey);
        var secondResult = await service.ChargeAsync(request, idempotencyKey);

        // Assert: the gateway (Stripe) was only ever actually charged once.
        // This is the assertion that actually protects the customer's money —
        // not just "one database row," but "one real charge attempt."
        gatewayMock.Verify(
            g => g.ChargeAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // Assert: both calls returned the same underlying transaction result.
        Assert.Equal(firstResult.TransactionId, secondResult.TransactionId);
        Assert.Equal(PaymentStatus.Succeeded.ToString(), firstResult.Status);
        Assert.Equal(firstResult.Status, secondResult.Status);

        // Assert: exactly one row exists in the database for this key.
        var matchingRows = dbContext.PaymentTransactions.Count(t => t.IdempotencyKey == idempotencyKey);
        Assert.Equal(1, matchingRows);
    }

    [Fact]
    public async Task ChargeAsync_DifferentIdempotencyKeys_ChargesGatewayTwice()
    {
        // Sanity-check counterpart: two genuinely different checkout
        // attempts (different keys) SHOULD both go through to the gateway.
        // This guards against an overly aggressive fix that accidentally
        // blocks legitimate repeat purchases.
        await using var dbContext = CreateInMemoryContext();
        var repository = new PaymentTransactionRepository(dbContext);

        var gatewayMock = new Mock<IPaymentGatewayClient>();
        gatewayMock
            .Setup(g => g.ChargeAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GatewayChargeResult { Success = true, ProviderTransactionId = "ch_test_456" });

        var service = new PaymentProcessingService(repository, gatewayMock.Object, NullLogger<PaymentProcessingService>.Instance);

        var request = new ChargeRequest
        {
            OrderId = Guid.NewGuid(),
            Amount = 20.00m,
            Currency = "usd",
            PaymentMethodToken = "tok_visa"
        };

        await service.ChargeAsync(request, "key-A");
        await service.ChargeAsync(request, "key-B");

        gatewayMock.Verify(
            g => g.ChargeAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}
