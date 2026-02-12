using FluentValidation;

namespace PointsWallet.Domain.Commands.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("User name should not be null or empty");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("User email should not be null or empty")
            .EmailAddress()
            .WithMessage("User email must be a valid email address");
    }
}
