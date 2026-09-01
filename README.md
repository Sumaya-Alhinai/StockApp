# StockApp — Auth + Product Catalog with Stock Management

.NET 9 + Angular 20: registration/login with JWT, and an authenticated product
catalog with movement-based stock management.

| Layer | Technology |
|---|---|
| Backend | .NET 9, Clean Architecture, MediatR (CQRS), FluentValidation, EF Core 9 |
| Database | SQL Server LocalDB |
| Auth | JWT bearer tokens, BCrypt password hashing |
| Frontend | Angular 20, standalone components, Reactive Forms, RxJS state |
| Tests | xUnit, 30 tests including a concurrent stock-adjustment test |

## Project structure

```
src/
  StockApp.Domain/          entities and enums, no dependencies
  StockApp.Application/     commands, queries, validators, interfaces
  StockApp.Infrastructure/  EF Core, DbContext, JWT, password hashing
  StockApp.API/             controllers, middleware, DI composition
tests/StockApp.Tests/       validator, handler and concurrency tests
stockapp-client/            Angular application
```

Dependencies point inward — `Domain` references nothing.

## Running

Prerequisites: .NET 9 SDK, Node 22, Angular CLI 20, SQL Server LocalDB.

```bash
# Backend
dotnet ef database update -p src/StockApp.Infrastructure -s src/StockApp.API
dotnet run --project src/StockApp.API           # http://localhost:5184

# Frontend
cd stockapp-client && npm install && ng serve   # http://localhost:4200
```

`dotnet test` requires LocalDB — each test class creates and drops its own database.

## Design decisions

**Cached `StockOnHand` column, not derived on read.** `RowVersion` only protects
a row that is actually written. Deriving stock from movements alone would never
touch `Product`, leaving the token inert and letting two concurrent stock-outs
both succeed. Instead the handler updates `Product` and inserts the
`StockMovement` in one `SaveChangesAsync`; the second request matches zero rows
and gets `409 CONCURRENCY_CONFLICT`. `StockMovement` stays the audit source of
truth. Verified by `Two_concurrent_stock_outs_cannot_both_succeed`.

**Delete and deactivate are separate commands.** A blocked delete changes
nothing; deactivation is an explicit follow-up the client offers after
`DELETE_BLOCKED`.

**One `AppDbContext`** — `Product.CreatedByUserId` is an FK to `User`, and EF Core
cannot model a relationship across two contexts.

**Validation in two places by design** — FluentValidation async rules give clear
messages; unique indexes are the actual guarantee.

## Error responses

All errors pass through `ExceptionHandlingMiddleware` and share one shape:

```json
{ "code": "INSUFFICIENT_STOCK", "message": "Cannot remove 999. Only 42 in stock.", "errors": null }
```

| Code | HTTP | Cause |
|---|---|---|
| `VALIDATION_ERROR` | 400 | FluentValidation failure, `errors` holds per-field messages |
| `INVALID_CREDENTIALS` | 401 | Wrong email or password (deliberately not distinguished) |
| `NOT_FOUND` | 404 | Product does not exist |
| `DUPLICATE_EMAIL` | 409 | Unique index on `User.Email` |
| `DUPLICATE_SKU` | 409 | Unique index on `Product.SKU` |
| `INSUFFICIENT_STOCK` | 409 | Stock-out exceeds stock on hand |
| `DELETE_BLOCKED` | 409 | Product has movement history; nothing changed, client may deactivate |
| `CONCURRENCY_CONFLICT` | 409 | `RowVersion` mismatch on stock adjustment |

The frontend switches on `code`, never on message text.

## Frontend

`authGuard` on `/products`, `productsResolver` pre-fetches the list, RxJS state
in `AuthService`/`ProductService`, search debounced 300ms with `switchMap`.
`ProductsPageComponent` is the only component that injects services; list, form
and stock panel are `@Input`/`@Output` only. Stock-out is capped at
`stockOnHand`; `INSUFFICIENT_STOCK` and `CONCURRENCY_CONFLICT` render as distinct
handled states.

## Known limitations

JWT key committed for review convenience · auth guard checks token presence, not
expiry · no refresh rotation or lockout (out of scope) · minimal styling.
