using EVChargingBackend.Application.Abstractions;
using EVChargingBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EVChargingBackend.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<ChargingSession> ChargingSessions => Set<ChargingSession>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Wallet>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.UserId).IsUnique();
        });

        modelBuilder.Entity<ChargingSession>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.EnergyKwh).HasPrecision(18, 4);
            b.Property(x => x.TariffRatePerKwh).HasPrecision(18, 4);
        });

        modelBuilder.Entity<WalletTransaction>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Amount).HasPrecision(18, 2);
            b.HasIndex(x => x.SessionId).IsUnique();
        });
    }
}