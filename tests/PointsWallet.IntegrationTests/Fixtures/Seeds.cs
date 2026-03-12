using PointsWallet.Domain.Models;
using PointsWallet.IntegrationTests.Fixtures;

public class Seeds(PointsWalletWebApplicationFixture fixture)
{
    public const string UserId = "testuser-id";
    public const string UserName = "Test User";
    public const string UserEmail = "testuser@example.com"; 
    public const string WalletId = "testwallet-id";

    private readonly PointsWalletWebApplicationFixture _fixture = fixture;

    public async Task SeedNewUser()
    {
        var user = new User(UserName, UserEmail) { Id = UserId };
        await _fixture.ExecuteDbContextAsync(async db => {
            db.Users.Add(user);
            await db.SaveChangesAsync();
        });
    }

    public async Task SeedWalletForUser(string userId)
    {
        var wallet = new Wallet(userId)
        {
            Id = WalletId
        };
        await _fixture.ExecuteDbContextAsync(async db => {
            db.Wallets.Add(wallet);
            await db.SaveChangesAsync();
        });
    }
}