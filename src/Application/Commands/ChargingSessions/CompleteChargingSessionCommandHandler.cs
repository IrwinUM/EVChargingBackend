using EVChargingBackend.Application.Abstractions;
using EVChargingBackend.Application.Common;
using EVChargingBackend.Domain.Entities;
using EVChargingBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EVChargingBackend.Application.Commands.ChargingSessions;

public class CompleteChargingSessionCommandHandler
{
    private readonly IAppDbContext _dbContext;

    public CompleteChargingSessionCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(
        CompleteChargingSessionCommand command,
        CancellationToken cancellationToken)
    {
        var session = await _dbContext.ChargingSessions
            .FirstOrDefaultAsync(x => x.Id == command.SessionId, cancellationToken);

        if (session == null)
            throw new Exception(ErrorMessages.SessionNotFound);

        var alreadyCharged = await _dbContext.WalletTransactions
            .AnyAsync(x => x.SessionId == session.Id, cancellationToken);

        if (alreadyCharged)
            throw new Exception(ErrorMessages.AlreadyCompleted);

        if (session.Status != ChargingSessionStatus.InProgress)
            throw new Exception(ErrorMessages.SessionNotInProgress);

        var cost = decimal.Round(
            session.EnergyKwh * session.TariffRatePerKwh,
            2,
            MidpointRounding.AwayFromZero);

        var credits = await _dbContext.WalletTransactions
            .Where(x => x.UserId == session.UserId && x.Type == TransactionType.Credit)
            .Select(x => (decimal?)x.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        var debits = await _dbContext.WalletTransactions
            .Where(x => x.UserId == session.UserId && x.Type == TransactionType.Debit)
            .Select(x => (decimal?)x.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        var balance = credits - debits;

        if (balance < cost)
            throw new Exception(ErrorMessages.InsufficientFunds);

        _dbContext.WalletTransactions.Add(new WalletTransaction
        {
            Id = Guid.NewGuid(),
            UserId = session.UserId,
            SessionId = session.Id,
            Amount = cost,
            Type = TransactionType.Debit,
            CreatedAt = DateTime.UtcNow
        });

        session.Status = ChargingSessionStatus.Completed;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}