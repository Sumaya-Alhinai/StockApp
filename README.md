# StockApp — Auth + Product Catalog with Stock Management

.NET 9 + Angular 20 implementation of user registration/login and an
authenticated product catalog with movement-based stock management.

## Stack

| Layer | Technology |
|---|---|
| Backend | .NET 9, Clean Architecture, MediatR (CQRS), FluentValidation, EF Core 9 |
| Database | SQL Server LocalDB |
| Auth | JWT bearer tokens, BCrypt password hashing |
| Frontend | Angular 20, standalone components, Reactive Forms, RxJS state |
| Tests | xUnit, 13 tests including a concurrent stock-adjustment test |

## Project structure


```
src/
  StockApp.Domain/          entities and enums, no dependencies
  StockApp.Application/     commands, queries, validators, interfaces
  StockApp.Infrastructure/  EF Core, DbContext, JWT, password hashing
  StockApp.API/             controllers, middleware, DI composition
tests/
  StockApp.Tests/           validator, handler and concurrency tests
stockapp-client/            Angular application
```



Dependencies point inward. `Domain` references nothing. `API` references
`Infrastructure` only to register services at startup — controllers depend
on interfaces defined in `Application`.

## Running

Prerequisites: .NET 9 SDK, Node 22, Angular CLI 20, SQL Server LocalDB.

```bash
# Backend
dotnet tool install --global dotnet-ef --version 9.0.0
dotnet ef database update -p src/StockApp.Infrastructure -s src/StockApp.API
dotnet run --project src/StockApp.API      # http://localhost:5184

# Frontend
cd stockapp-client
npm install
ng serve                                    # http://localhost:4200
```

Run tests with `dotnet test` (requires LocalDB — each test creates and
drops its own database).

## Design decisions

### Stock on hand: cached column, not derived on read

The specification allows either approach. I chose a `StockOnHand` column on
`Product`, updated inside the same transaction as each `StockMovement` insert.

The reason is concurrency. The data model specifies `RowVersion` on `Product`
as a concurrency token, but that token only protects a row that is actually
written. If stock were derived purely by summing movements, a stock-out would
insert a movement row and never touch `Product` — leaving `RowVersion` inert.
Two simultaneous stock-outs would both read stock as 10, both pass the
sufficiency check, and both succeed, driving stock to -6.

With the cached column, `AdjustStockCommandHandler` modifies `Product` and adds
the `StockMovement` in a single `SaveChangesAsync`. EF Core appends
`AND RowVersion = @old` to the UPDATE. The second request matches zero rows,
throws `DbUpdateConcurrencyException`, and the handler returns
`409 CONCURRENCY_CONFLICT`.

`StockMovement` remains the audit source of truth; the column is a
transactionally-maintained cache for reads and validation.

Verified by `ConcurrentStockAdjustmentTests.Two_concurrent_stock_outs_cannot_both_succeed`:
two contexts read stock 10, both attempt to remove 8, exactly one succeeds,
final stock is 2, one movement recorded.

### Single DbContext

The specification mentions a dedicated `DbContext` in both Part A and Part B.
I used one `AppDbContext` covering all three entities, because
`Product.CreatedByUserId` is a foreign key to `User` and EF Core cannot model
a foreign-key relationship across two separate contexts.

### Guid identifiers

Ids are generated in the handler before saving, and do not leak record counts
through the API.

### Validation lives in two places by design

FluentValidation async rules (unique email, unique SKU) produce clear
user-facing messages, but there is a window between the check and the save.
Unique indexes in the database are the actual guarantee; handlers catch
`DbUpdateException` and return a structured conflict.

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
| `DELETE_BLOCKED` | 409 | Product has movement history; deactivated instead |
| `CONCURRENCY_CONFLICT` | 409 | `RowVersion` mismatch on stock adjustment |

The frontend switches on `code`, never on message text.

## Frontend notes

- `authGuard` protects `/products`; unauthenticated users redirect to `/login`
- `productsResolver` pre-fetches the list before route activation
- `AuthService` and `ProductService` expose state as RxJS observables
  (`BehaviorSubject`) for list, search term, movements, loading and error
- Global search is debounced 300ms with `switchMap` to cancel in-flight requests
- `ProductsPageComponent` is the only component that injects services;
  `ProductListComponent`, `ProductFormComponent` and `StockPanelComponent`
  communicate purely through `@Input`/`@Output`
- The stock-out quantity field applies `Validators.max(stockOnHand)` only when
  movement type is `Out`
- `INSUFFICIENT_STOCK` and `CONCURRENCY_CONFLICT` render as distinct
  handled states rather than generic errors

## Known limitations

- JWT signing key is committed in `appsettings.json` for review convenience;
  production would source it from environment variables or a secret store
- No refresh token rotation or account lockout (explicitly out of scope)
- Frontend styling is minimal — the focus is on the required architectural patterns