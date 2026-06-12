# EV Charging Backend

## Overview

This project implements a small EV charging backend service for completing charging sessions and querying wallet balances.

The service follows the assignment requirements with:

- Clean Architecture style separation
- CQRS with separate command and query handlers
- PostgreSQL persistence
- FluentValidation for request validation
- Unit tests
- Integration test against a real PostgreSQL database

## Features

### Complete charging session

`POST /sessions/{id}/complete`

Completes an in-progress charging session by:

- validating the session exists
- validating the session is still `InProgress`
- calculating the session cost as `energyKwh * tariffRatePerKwh`
- rounding the cost to 2 decimal places
- computing the wallet balance from the transaction ledger
- rejecting the operation if funds are insufficient
- writing a debit transaction
- marking the session as completed

### Get wallet balance

`GET /wallets/{userId}/balance`

Returns the current computed wallet balance for a user based on wallet transactions.

## Tech Stack

- .NET 10
- ASP.NET Core Minimal API
- Entity Framework Core
- PostgreSQL
- FluentValidation
- xUnit
- FluentAssertions
- Testcontainers

## How to Run

### 1. Start PostgreSQL

This project uses PostgreSQL in Docker.

Run:

```bash
docker run --name evcharging-postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=evchargingdb \
  -p 5432:5432 \
  -d postgres:16
```

If the container already exists but is stopped, run:

```bash
docker start evcharging-postgres
```

If the old container is unusable, create a new one with a different name:

```bash
docker run --name evcharging-postgres-3 \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=evchargingdb \
  -p 5432:5432 \
  -d postgres:16
```

### 2. Build the API

```bash
dotnet build src/API/EVChargingBackend.API.csproj
```

### 3. Run the API

```bash
dotnet run --project src/API/EVChargingBackend.API.csproj
```

By default, the API uses the PostgreSQL connection string in `src/API/appsettings.json`.

## API Endpoints

### Complete session

```http
POST /sessions/{id}/complete
```

Responses:

- `204 No Content` when completion succeeds
- `404 Not Found` if the session does not exist
- `409 Conflict` if the session is not in progress or has already been completed
- `422 Unprocessable Entity` if the wallet has insufficient funds

### Get wallet balance

```http
GET /wallets/{userId}/balance
```

Responses:

- `200 OK` with the computed balance
- `404 Not Found` if the wallet does not exist

## Manual Demo Steps

### 1. Keep the API running

```bash
dotnet run --project src/API/EVChargingBackend.API.csproj
```

### 2. Seed demo data into PostgreSQL

If your running container is `evcharging-postgres-3`, run:

```bash
docker exec -i evcharging-postgres-3 psql -U postgres -d evchargingdb <<'SQL'
INSERT INTO "Wallets" ("Id", "UserId")
VALUES ('11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222');

INSERT INTO "ChargingSessions" ("Id", "UserId", "EnergyKwh", "TariffRatePerKwh", "Status")
VALUES ('33333333-3333-3333-3333-333333333333', '22222222-2222-2222-2222-222222222222', 10.0, 2.5, 0);

INSERT INTO "WalletTransactions" ("Id", "UserId", "SessionId", "Amount", "Type", "CreatedAt")
VALUES ('44444444-4444-4444-4444-444444444444', '22222222-2222-2222-2222-222222222222', NULL, 100.00, 1, NOW());
SQL
```

This seeds:
- one wallet for the user
- one charging session in `InProgress`
- one credit transaction of `100.00`

### 3. Test the command endpoint

Run:

```bash
curl -i -X POST http://localhost:5209/sessions/33333333-3333-3333-3333-333333333333/complete
```

Expected result:

```http
HTTP/1.1 204 No Content
```

This shows the charging session was successfully completed.

### 4. Test the query endpoint

Run:

```bash
curl http://localhost:5209/wallets/22222222-2222-2222-2222-222222222222/balance
```

Expected result:

```json
{"userId":"22222222-2222-2222-2222-222222222222","balance":75.0}
```

Explanation:
- initial wallet credit = `100.00`
- charging cost = `10 * 2.5 = 25.00`
- remaining balance = `75.00`

### 5. Test idempotency

Run the same complete-session request again:

```bash
curl -i -X POST http://localhost:5209/sessions/33333333-3333-3333-3333-333333333333/complete
```

Expected result:
- a conflict-style response instead of another successful charge

This demonstrates that the same session is not charged twice.

## How to Test

### Unit tests

```bash
dotnet test tests/UnitTests/EVChargingBackend.UnitTests.csproj
```

### Integration tests

```bash
dotnet test tests/IntegrationTests/EVChargingBackend.IntegrationTests.csproj
```

The integration test uses a real PostgreSQL container through Testcontainers.

## Assumptions

- One wallet belongs to one user
- Wallet balance is never stored directly
- Wallet balance is always computed from the ledger of wallet transactions
- Credit transactions may exist without a charging session
- A charging session can only be completed once
- Session completion is treated as idempotent through the unique `SessionId` constraint on wallet transactions
- Monetary values are stored as `decimal`
- Session cost is rounded to 2 decimal places using `MidpointRounding.AwayFromZero`

## What I Would Improve With More Time

- Replace generic `Exception` usage with custom domain or application exception types
- Add EF Core migrations instead of relying on `EnsureCreated`
- Add more integration tests for failure scenarios
- Add stronger concurrency protection for double-spend cases
- Add logging and more structured error responses
- Remove tracked `bin` and `obj` build artifacts from the repository history and keep them fully ignored

## Notes

This implementation focuses only on the requested backend slice.

Out of scope:

- authentication
- payment gateway integration
- UI
- advanced production hardening
