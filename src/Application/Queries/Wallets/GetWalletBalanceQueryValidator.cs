using FluentValidation;

namespace EVChargingBackend.Application.Queries.Wallets;

public class GetWalletBalanceQueryValidator : AbstractValidator<GetWalletBalanceQuery>
{
    public GetWalletBalanceQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}