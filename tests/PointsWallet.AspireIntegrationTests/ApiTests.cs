using System.Net.Http.Json;
using FluentAssertions;
using PointsWallet.Api.Endpoints;
using PointsWallet.Api.Requests.Users;
using PointsWallet.Api.Requests.Wallets;
using PointsWallet.AspireIntegrationTests.Fixtures;

namespace PointsWallet.AspireIntegrationTests;

[Collection(nameof(AspireAppCollection))]
public class ApiTests(AspireAppFixture fixture)
{
    private readonly HttpClient _client = fixture.ApiClient;   

    #region Users Tests

    [Fact]
    public async Task CreateUserAsync_ShouldCallCommandAndReturnSuccess()
    {
        // Arrange
        var request = new CreateUserRequest("John Doe", "john.doe@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/users", request);

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<CreateUserResponse>();
        content.Should().NotBeNull();
        content?.UserId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetUsersAsync_ShouldReturnOkStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.EnsureSuccessStatusCode();
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Wallets Tests

    [Theory]
    [InlineData("My Wallet")]
    [InlineData(null)]
    public async Task CreateWalletAsync_ShouldCallCommandAndReturnSuccess(
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

    [Fact]
    public async Task AddPointsAsync_ShouldSendCommandAndReturnSuccess()
    {
        // Arrange
        var userId = await CreateUser();

        var walletResponse = await _client.PostAsJsonAsync($"/api/users/{userId}/wallets", new CreateWalletRequest("My Wallet"));
        walletResponse.EnsureSuccessStatusCode();

        var walletContent = await walletResponse.Content.ReadFromJsonAsync<CreateWalletResponse>();
        var walletId = walletContent?.WalletId;
        walletId.Should().NotBeNullOrEmpty();

        var request = new AddPointsRequest(100);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/users/{userId}/wallets/{walletId}/points", request);

        // Assert
        response.EnsureSuccessStatusCode();
    }
    
    #endregion

    #region Private Helpers

    private async Task<string> CreateUser()
    {
        var request = new CreateUserRequest("John Doe", "john.doe@example.com");

        var response = await _client.PostAsJsonAsync("/api/users", request);

        var content = await response.Content.ReadFromJsonAsync<CreateUserResponse>();
        content.Should().NotBeNull();
        content?.UserId.Should().NotBeNullOrEmpty();

        return content!.UserId;
    }

    #endregion
}
