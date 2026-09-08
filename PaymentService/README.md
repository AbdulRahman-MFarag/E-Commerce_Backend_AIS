# Payment Service

Owns all payment processing for the e-commerce system. This service is the only
one that ever talks to the payment provider (Stripe, test mode) — Order Service
calls this service, never Stripe directly.

## Structure (Clean Architecture)

```
src/
  PaymentService.Domain          <- PaymentTransaction entity, PaymentStatus enum.
                                     No dependencies on anything else.
  PaymentService.Application     <- DTOs, interfaces, and PaymentProcessingService
                                     (the idempotency-safe charge logic).
                                     Depends only on Domain.
  PaymentService.Infrastructure  <- EF Core DbContext (the DB actually lives
                                     here, not in Domain), repository, and the
                                     Stripe test-mode gateway client.
                                     Depends on Application (implements its interfaces).
  PaymentService.Presentation    <- Controllers, Program.cs, JWT auth, rate
                                     limiting, Serilog. Depends on Infrastructure.
tests/
  PaymentService.Tests           <- The idempotency test called out specifically
                                     in the requirements document.
```

Dependencies point inward only: Presentation -> Infrastructure -> Application -> Domain.
Domain and Application know nothing about EF Core, ASP.NET, or Stripe.

## The one file to actually understand deeply

`src/PaymentService.Application/Services/PaymentProcessingService.cs`

This implements the idempotency walkthrough discussed in the design docs:
check for an existing idempotency key, insert a `Pending` row *before* calling
the gateway, call the gateway, then update the row to its real final status.
The ordering (insert before call) is what prevents a crash mid-charge from
leaving a real charge with no record of it.

## Why `OrderId` has no foreign key

`PaymentTransaction.OrderId` is a plain `Guid` column. There is no `Orders`
table in this service's database to relate it to — Order Service owns that
data in its own, completely separate database. The value arrives via the API
request body when Order Service calls `POST /api/v1/payments/charge`, and is
trusted because Order Service is the only caller of this endpoint. See
`Microservices_Data_Relationships.md` in the project docs for the full
reasoning behind this pattern.

## Running locally (once you have NuGet access)

```bash
cd PaymentService
dotnet restore
dotnet build

# apply the EF Core migration (creates PaymentTransactions table with the
# unique index on IdempotencyKey)
cd src/PaymentService.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../PaymentService.Presentation
dotnet ef database update --startup-project ../PaymentService.Presentation

cd ../../src/PaymentService.Presentation
dotnet run
```

## Running the tests

```bash
cd tests/PaymentService.Tests
dotnet test
```

The key test, `ChargeAsync_CalledTwiceWithSameIdempotencyKey_OnlyCallsGatewayOnce`,
asserts the mocked Stripe client is invoked **exactly once** even when the
charge endpoint is called twice with the same idempotency key — proving the
double-charge protection actually works, not just that the code exists.

## Before you commit

`appsettings.json` currently has placeholder values for the JWT signing key
and connection string. Move the real signing key to an environment variable
or `appsettings.Development.json` (already in `.gitignore`) before pushing —
never commit a real secret, even a shared dev one.

## Endpoints

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/v1/payments/charge` | Called by Order Service during checkout. Requires an `Idempotency-Key` header. |
| POST | `/api/v1/payments/{id}/refund` | Compensating action for the "payment succeeded, order failed" case. |
