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

    public DbSet<ChargingSession> ChargingSessions => Set<ChargingSession>();

    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WalletTransaction>()
            .HasIndex(x => x.SessionId)
            .IsUnique();
    }
}