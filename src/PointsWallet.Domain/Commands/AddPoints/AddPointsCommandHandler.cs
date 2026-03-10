using MediatR;
using Microsoft.Extensions.Logging;
using PointsWallet.Contracts.Events;
using PointsWallet.Domain.Events;
using PointsWallet.Domain.Repositories;

namespace PointsWallet.Domain.Commands.AddPoints;

public sealed class AddPointsCommandHandler(
    IWalletRepository walletRepository,
    IEventPublisher eventPublisher,
    ILogger<AddPointsCommandHandler> logger) : IRequestHandler<AddPointsCommand>
{
    public async Task Handle(AddPointsCommand request, CancellationToken cancellationToken)
    {
        var wallet = await walletRepository.GetByIdAsync(request.WalletId, cancellationToken)
            ?? throw new InvalidOperationException($"Wallet with id '{request.WalletId}' was not found");

        wallet.AddPoints(request.Points);
        await walletRepository.UpdateAsync(wallet, cancellationToken);

        var integrationEvent = new PointsAddedIntegrationEvent(
            wallet.Id,
            wallet.UserId,
            request.Points,
            wallet.Points,
            DateTime.UtcNow);

        await eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        logger.LogInformation(
            "[Add Points] Added {Points} points to Wallet {WalletId} for User {UserId}. New balance: {NewBalance}. CorrelationId: {CorrelationId}",
            request.Points,
            wallet.Id,
            wallet.UserId,
            wallet.Points,
            request.CorrelationId);
    }
}
