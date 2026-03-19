using Npgsql;
using PointsWallet.Domain.Models;

namespace PointsWallet.AspireIntegrationTests.Fixtures;

public class Seeds(AspireAppFixture fixture)
{
    public const string UserId = "testuser-id";
    public const string UserName = "Test User";
    public const string UserEmail = "testuser@example.com"; 
    public const string WalletId = "testwallet-id";

    private readonly AspireAppFixture _fixture = fixture;

    public async Task SeedNewUser()
    {
        var user = new User(UserName, UserEmail) { Id = UserId };
        await _fixture.ExecuteDbQueryAsync(async conn => {
            await using var cmd = new NpgsqlCommand("INSERT INTO Users (Id, Name, Email) VALUES (@Id, @Name, @Email)", conn);
            cmd.Parameters.AddWithValue("Id", UserId);
            cmd.Parameters.AddWithValue("Name", UserName);
            cmd.Parameters.AddWithValue("Email", UserEmail);
            await cmd.ExecuteNonQueryAsync();
        });
    }

    public async Task SeedWalletForUser(string userId)
    {
        var wallet = new Wallet(userId) { Id = WalletId };
        await _fixture.ExecuteDbQueryAsync(async conn => {
            await using var cmd = new NpgsqlCommand("INSERT INTO Wallets (Id, UserId) VALUES (@Id, @UserId)", conn);
            cmd.Parameters.AddWithValue("Id", WalletId);
            cmd.Parameters.AddWithValue("UserId", userId);
            await cmd.ExecuteNonQueryAsync();
        });
    }
}