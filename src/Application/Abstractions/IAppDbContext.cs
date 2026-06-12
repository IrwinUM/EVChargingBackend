using EVChargingBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EVChargingBackend.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Wallet> Wallets { get; }

    DbSet<ChargingSession> ChargingSessions { get; }

    DbSet<WalletTransaction> WalletTransactions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}