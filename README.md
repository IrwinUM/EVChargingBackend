# EV Charging Backend

## Features

- Complete Charging Session
- Wallet Balance Query
- PostgreSQL
- CQRS
- Clean Architecture
- FluentValidation
- Unit Tests
- Integration Tests

## Assumptions

- One wallet per user
- Balance computed from ledger
- Monetary values stored as decimal
- Sessions can only be completed once

## Idempotency

Wallet transactions store SessionId.

A unique database constraint prevents duplicate charges.
