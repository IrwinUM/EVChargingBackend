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