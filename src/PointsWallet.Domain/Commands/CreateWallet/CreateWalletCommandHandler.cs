using MediatR;
using PointsWallet.Domain.Models;
using PointsWallet.Domain.Repositories;

namespace PointsWallet.Domain.Commands.CreateWallet;

public sealed class CreateWalletCommandHandler(IWalletRepository walletRepository) : IRequestHandler<CreateWalletCommand, string>
{
    public async Task<string> Handle(CreateWalletCommand request, CancellationToken cancellationToken)
    {
        var existingWallets = await walletRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (existingWallets.Any(x => x.SymbolicName == request.SymbolicName))
            throw new InvalidOperationException($"A wallet with SymbolicName '{request.SymbolicName}' already exists for this user");

        var wallet = new Wallet(request.UserId, request.SymbolicName);
        await walletRepository.AddAsync(wallet, cancellationToken);

        return wallet.Id;
    }
}
