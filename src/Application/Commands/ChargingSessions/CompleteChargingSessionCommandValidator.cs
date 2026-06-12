using FluentValidation;

namespace EVChargingBackend.Application.Commands.ChargingSessions;

public class CompleteChargingSessionCommandValidator : AbstractValidator<CompleteChargingSessionCommand>
{
    public CompleteChargingSessionCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty();
    }
}