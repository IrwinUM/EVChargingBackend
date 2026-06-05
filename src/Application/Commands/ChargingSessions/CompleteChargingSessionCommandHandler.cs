using EVChargingBackend.Domain.Enums;
using EVChargingBackend.Domain.Entities;
using EVChargingBackend.Application.Common;

namespace EVChargingBackend.Application.Commands.ChargingSessions;

public class CompleteChargingSessionCommandHandler
{
    private readonly List<ChargingSession> _sessions;
    private readonly List<WalletTransaction> _transactions;

    public CompleteChargingSessionCommandHandler(
        List<ChargingSession> sessions,
        List<WalletTransaction> transactions)
    {
        _sessions = sessions;
        _transactions = transactions;
    }

    public void Handle(CompleteChargingSessionCommand command)
    {
        var session = _sessions.FirstOrDefault(x => x.Id == command.SessionId);

        if (session == null)
            throw new Exception(ErrorMessages.SessionNotFound);

        if (session.Status != ChargingSessionStatus.InProgress)
            throw new Exception(ErrorMessages.SessionNotInProgress);

        // 🔥 Idempotency check
        var alreadyCharged = _transactions.Any(x => x.SessionId == session.Id);

        if (alreadyCharged)
            throw new Exception(ErrorMessages.AlreadyCompleted);

        var cost = session.EnergyKwh * session.TariffRatePerKwh;

        var userTransactions = _transactions
            .Where(x => x.UserId == session.UserId);

        var balance =
            userTransactions.Where(x => x.Type == TransactionType.Credit).Sum(x => x.Amount)
            -
            userTransactions.Where(x => x.Type == TransactionType.Debit).Sum(x => x.Amount);

        if (balance < cost)
            throw new Exception(ErrorMessages.InsufficientFunds);

        // Debit transaction
        _transactions.Add(new WalletTransaction
        {
            Id = Guid.NewGuid(),
            UserId = session.UserId,
            SessionId = session.Id,
            Amount = cost,
            Type = TransactionType.Debit,
            CreatedAt = DateTime.UtcNow
        });

        session.Status = ChargingSessionStatus.Completed;
    }
}