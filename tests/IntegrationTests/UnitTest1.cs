using System.Net;
using System.Net.Http.Json;
using EVChargingBackend.Domain.Entities;
using EVChargingBackend.Domain.Enums;
using EVChargingBackend.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EVChargingBackend.IntegrationTests;

public class ChargingSessionEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ChargingSessionEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CompleteSession_ThenGetBalance_UpdatesLedgerAndReturnsComputedBalance()
    {
        var client = _factory.CreateClient();

        var userId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.Wallets.Add(new Wallet
            {
                Id = walletId,
                UserId = userId
            });

            dbContext.ChargingSessions.Add(new ChargingSession
            {
                Id = sessionId,
                UserId = userId,
                EnergyKwh = 10m,
                TariffRatePerKwh = 2.5m,
                Status = ChargingSessionStatus.InProgress
            });

            dbContext.WalletTransactions.Add(new WalletTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionId = null,
                Amount = 100m,
                Type = TransactionType.Credit,
                CreatedAt = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        var completeResponse = await client.PostAsync($"/sessions/{sessionId}/complete", content: null);

        completeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var balanceResponse = await client.GetAsync($"/wallets/{userId}/balance");

        balanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var balance = await balanceResponse.Content.ReadFromJsonAsync<GetWalletBalanceResponse>();

        balance.Should().NotBeNull();
        balance!.UserId.Should().Be(userId);
        balance.Balance.Should().Be(75m);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var session = await verifyDbContext.ChargingSessions
            .SingleAsync(x => x.Id == sessionId);

        var debitTransaction = await verifyDbContext.WalletTransactions
            .SingleAsync(x => x.SessionId == sessionId && x.Type == TransactionType.Debit);

        session.Status.Should().Be(ChargingSessionStatus.Completed);
        debitTransaction.Amount.Should().Be(25m);
    }

    private sealed record GetWalletBalanceResponse(Guid UserId, decimal Balance);
}