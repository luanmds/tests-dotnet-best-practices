using MediatR;
using PointsWallet.Domain.Repositories;

namespace PointsWallet.Domain.Models.Commands;

public sealed class CreateUserCommandHandler(IUserRepository userRepository) : IRequestHandler<CreateUserCommand, string>
{
    public async Task<string> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User(request.Name, request.Email);
        await userRepository.AddAsync(user, cancellationToken);
        return user.Id;
    }
}
