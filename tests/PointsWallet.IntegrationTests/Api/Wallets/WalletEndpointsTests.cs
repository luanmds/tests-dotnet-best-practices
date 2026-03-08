using System.Net.Http.Json;
using FluentAssertions;
using PointsWallet.Api.Endpoints;
using PointsWallet.Api.Requests.Wallets;
using PointsWallet.Domain.Models;
using PointsWallet.IntegrationTests.Fixtures;

namespace PointsWallet.IntegrationTests.Api.Wallets;

public class WalletEndpointsTests(
    PointsWalletWebApplicationFixture fixture)
    : IClassFixture<PointsWalletWebApplicationFixture>
{
    private readonly HttpClient _client = fixture.Client;
    private readonly PointsWalletWebApplicationFixture _fixture = fixture;

    [Theory]
    [InlineData("My Wallet")]
    [InlineData(null)]
    public async Task CreateWalletRequest_ShouldCallCommandAndReturnSuccess(
        string? symbolicName)
    {
        // Arrange
        var userId = await CreateUser();
        var request = new CreateWalletRequest(symbolicName);

        // Act        
        var response = await _client.PostAsJsonAsync($"/api/users/{userId}/wallets", request);

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<CreateWalletResponse>();
        content.Should().NotBeNull();
        content?.WalletId.Should().NotBeNullOrEmpty();
    }

    private async Task<string> CreateUser()
    {
        var user = new User("Test User", "test.user@example.com");
        await _fixture.ExecuteDbContextAsync(async db => {
            db.Users.Add(user);
            await db.SaveChangesAsync();
        });
        return user.Id;
    }
}