using EVChargingBackend.Application.Abstractions;
using EVChargingBackend.Application.Common;
using EVChargingBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EVChargingBackend.Application.Queries.Wallets;

public class GetWalletBalanceQueryHandler
{
    private readonly IAppDbContext _dbContext;

    public GetWalletBalanceQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetWalletBalanceResult> Handle(
        GetWalletBalanceQuery query,
        CancellationToken cancellationToken)
    {
        var walletExists = await _dbContext.Wallets
            .AnyAsync(x => x.UserId == query.UserId, cancellationToken);

        if (!walletExists)
            throw new Exception(ErrorMessages.WalletNotFound);

        var credits = await _dbContext.WalletTransactions
            .Where(x => x.UserId == query.UserId && x.Type == TransactionType.Credit)
            .Select(x => (decimal?)x.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        var debits = await _dbContext.WalletTransactions
            .Where(x => x.UserId == query.UserId && x.Type == TransactionType.Debit)
            .Select(x => (decimal?)x.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        var balance = credits - debits;

        return new GetWalletBalanceResult(query.UserId, balance);
    }
}