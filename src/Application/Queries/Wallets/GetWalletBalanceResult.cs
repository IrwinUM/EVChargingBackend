namespace EVChargingBackend.Application.Queries.Wallets;

public record GetWalletBalanceResult(Guid UserId, decimal Balance);