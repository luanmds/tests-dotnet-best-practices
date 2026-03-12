using FluentValidation;

namespace PointsWallet.Domain.Commands.CreateWallet;

public sealed class CreateWalletCommandValidator : AbstractValidator<CreateWalletCommand>
{
    public CreateWalletCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("Wallet user id should not be null or empty");

        RuleFor(x => x.SymbolicName)
            .MaximumLength(100)
            .WithMessage("Wallet symbolic name should have a maximum length of 100 characters");

        RuleFor(x => x.SymbolicName)
            .Must(symbolicName => symbolicName is null || !string.IsNullOrWhiteSpace(symbolicName))
            .WithMessage("Wallet symbolic name should not be empty when provided");
    }
}
