using EVChargingBackend.Application.Commands.ChargingSessions;
using EVChargingBackend.Application.Common;
using EVChargingBackend.Domain.Entities;
using EVChargingBackend.Domain.Enums;
using EVChargingBackend.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EVChargingBackend.UnitTests;

public class CompleteChargingSessionCommandHandlerTests
{
    [Fact]
    public async Task Handle_CompletesSessionAndCreatesDebit_WhenBalanceIsSufficient()
    {
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        dbContext.ChargingSessions.Add(CreateSession(sessionId, userId, 10m, 2.5m, ChargingSessionStatus.InProgress));
        dbContext.WalletTransactions.Add(CreateCredit(userId, 50m));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new CompleteChargingSessionCommandHandler(dbContext);

        await handler.Handle(new CompleteChargingSessionCommand(sessionId), CancellationToken.None);

        var session = await dbContext.ChargingSessions.SingleAsync(x => x.Id == sessionId);
        var debit = await dbContext.WalletTransactions.SingleAsync(x => x.SessionId == sessionId && x.Type == TransactionType.Debit);

        session.Status.Should().Be(ChargingSessionStatus.Completed);
        debit.Amount.Should().Be(25m);
    }

    [Fact]
    public async Task Handle_Throws_WhenSessionIsNotInProgress()
    {
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        dbContext.ChargingSessions.Add(CreateSession(sessionId, userId, 10m, 2.5m, ChargingSessionStatus.Completed));
        dbContext.WalletTransactions.Add(CreateCredit(userId, 50m));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new CompleteChargingSessionCommandHandler(dbContext);

        var act = () => handler.Handle(new CompleteChargingSessionCommand(sessionId), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage(ErrorMessages.SessionNotInProgress);
    }

    [Fact]
    public async Task Handle_Throws_WhenFundsAreInsufficient()
    {
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        dbContext.ChargingSessions.Add(CreateSession(sessionId, userId, 10m, 2.5m, ChargingSessionStatus.InProgress));
        dbContext.WalletTransactions.Add(CreateCredit(userId, 20m));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new CompleteChargingSessionCommandHandler(dbContext);

        var act = () => handler.Handle(new CompleteChargingSessionCommand(sessionId), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage(ErrorMessages.InsufficientFunds);

        var session = await dbContext.ChargingSessions.SingleAsync(x => x.Id == sessionId);
        session.Status.Should().Be(ChargingSessionStatus.InProgress);
        dbContext.WalletTransactions.Count(x => x.Type == TransactionType.Debit).Should().Be(0);
    }

    [Fact]
    public async Task Handle_RoundsCostToTwoDecimalPlaces()
    {
        using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        dbContext.ChargingSessions.Add(CreateSession(sessionId, userId, 0.67m, 1.5m, ChargingSessionStatus.InProgress));
        dbContext.WalletTransactions.Add(CreateCredit(userId, 10m));
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new CompleteChargingSessionCommandHandler(dbContext);

        await handler.Handle(new CompleteChargingSessionCommand(sessionId), CancellationToken.None);

        var debit = await dbContext.WalletTransactions.SingleAsync(x => x.SessionId == sessionId && x.Type == TransactionType.Debit);
        debit.Amount.Should().Be(1.01m);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static ChargingSession CreateSession(
        Guid sessionId,
        Guid userId,
        decimal energyKwh,
        decimal tariffRatePerKwh,
        ChargingSessionStatus status)
    {
        return new ChargingSession
        {
            Id = sessionId,
            UserId = userId,
            EnergyKwh = energyKwh,
            TariffRatePerKwh = tariffRatePerKwh,
            Status = status
        };
    }

    private static WalletTransaction CreateCredit(Guid userId, decimal amount)
    {
        return new WalletTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionId = null,
            Amount = amount,
            Type = TransactionType.Credit,
            CreatedAt = DateTime.UtcNow
        };
    }
}