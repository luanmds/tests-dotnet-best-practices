using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using PointsWallet.Contracts.Messages;
using PointsWallet.Domain.Models;
using PointsWallet.IntegrationTests.Fixtures;

namespace PointsWallet.IntegrationTests.Worker;

public class AddPointsCommandHandlerTests(PointsWalletWebApplicationFixture fixture) 
    : IClassFixture<PointsWalletWebApplicationFixture>
{

    [Fact]
    public async Task AddPointsCommandHandler_ShouldAddPointsAndPublishEvent()
    {
        var pointsToAdd = 100;

        var publishEndpoint = fixture.Services.GetRequiredService<IPublishEndpoint>();
        var commandMessage = new AddPointsMessage(
            Seeds.WalletId, Seeds.UserId, pointsToAdd, Guid.NewGuid().ToString());

        await publishEndpoint.Publish(commandMessage);

        var wallet = await WaitForWalletAsync(Seeds.WalletId, pointsToAdd, TimeSpan.FromSeconds(3));

        Assert.NotNull(wallet);
        Assert.Equal(pointsToAdd, wallet!.Points);
    }

    private async Task<Wallet?> WaitForWalletAsync(
        string walletId,
        long expectedPoints,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline)
        {
            Wallet? foundWallet = null;

            await fixture.ExecuteDbContextAsync(async dbContext =>
            {
                var currentWallet = await dbContext.Wallets.FindAsync(walletId);
                if (currentWallet is not null && currentWallet.Points == expectedPoints)
                {
                    foundWallet = currentWallet;
                }
            });

            if (foundWallet is not null)
            {
                return foundWallet;
            }

            await Task.Delay(200);
        }

        return null;
    }
}