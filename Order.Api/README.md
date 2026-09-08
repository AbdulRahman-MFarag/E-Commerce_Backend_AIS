# Order.Api

Order microservice for the e-commerce backend. ASP.NET Core 8 + EF Core + SQLite.

## Stack / what's inside

- JWT auth on all order endpoints, ownership check so a user only sees their own orders
- Idempotency-Key header on create, so retrying a checkout doesn't create a duplicate order
- Order status machine (PendingPayment -> Paid -> Confirmed -> Shipped -> Delivered, plus cancel/fail paths)
- Product price/name are pulled from Catalog server-side, not trusted from the request body
- 30s in-memory cache on `GET /orders`
- Swagger + a dev-only token endpoint so you can test without a real Identity server
- `Order.Api.http` has a full click-through test flow

## Run

```bash
dotnet restore
dotnet run
```

Swagger opens automatically in dev. SQLite db (`orders.db`) is created on first run.

Quick test:
1. `POST /api/v1/dev/token?userId=demo-user` to get a token (dev only)
2. Authorize in Swagger with `Bearer <token>`
3. `POST /api/v1/orders` with an `Idempotency-Key` header and items
4. `GET /api/v1/orders`, `PATCH /api/v1/orders/{id}/status`

## Auth

Expects a JWT with `sub` (or NameIdentifier) claim.

- If `Jwt:Key` is set in config, tokens are validated against that local key (demo mode, used right now) and the dev token endpoint is enabled.
- To point it at the real auth service instead: remove `Jwt:Key`, set `Jwt:Authority` + `Jwt:Audience`, and drop the dev token endpoint in `Program.cs` before shipping.

## Endpoints

**POST /api/v1/orders** — header `Idempotency-Key: <unique>`, body:
```json
{ "items": [ { "productId": "11111111-1111-1111-1111-111111111111", "quantity": 1 } ] }
```
Note: the client only sends `productId` + `quantity`. Price and product name are looked up from Catalog (`ICatalogService`), never taken from the request — otherwise a client could set its own price.

Response includes `amount` (decimal) and `currency` (ISO 4217, e.g. `"EGP"`) — both come from Catalog at checkout time, never from the client, so Payment can trust them directly. All items in one order must share the same currency (order fails with 400 if they don't).

**GET /api/v1/orders** — current user's orders, cached 30s.

**GET /api/v1/orders/{id}** — single order, ownership enforced.

**PATCH /api/v1/orders/{id}/status** — body `{ "status": "Paid" }`.

## Integrating with the rest of the team

- Keep the route `/api/v1/orders` as is so Cart/Payment can integrate against it.
- Swap `InMemoryCatalogService` for a real call to Catalog (e.g. `GET /internal/products/{id}`) — same `ICatalogService` interface, just a different implementation.
- Use the team's real JWT/Identity config instead of the local dev key.
- `amount` + `currency` on the order response are always server-computed/looked-up from Catalog, so Payment can trust them directly.

## Notes

- `orders.db` is a local file, fine for dev, swap the connection string for the real DB when merging.
- Real payment gateway calls are out of scope here — Order just tracks status (`Paid`, `PaymentFailed`, etc.), Payment service is what actually charges the customer.
