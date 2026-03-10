using FluentValidation;

namespace PointsWallet.Domain.Commands.AddPoints;

public sealed class AddPointsCommandValidator : AbstractValidator<AddPointsCommand>
{
    public AddPointsCommandValidator()
    {
        RuleFor(x => x.WalletId)
            .NotEmpty()
            .WithMessage("Wallet id should not be null or empty");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User id should not be null or empty");

        RuleFor(x => x.Points)
            .GreaterThan(0)
            .WithMessage("Points must be greater than zero");

        RuleFor(x => x.CorrelationId)
            .NotEmpty()
            .WithMessage("Correlation id should not be null or empty");
    }
}
