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

    // [Fact]
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

    // [Theory]
    // [InlineData("My Wallet")]
    // [InlineData(null)]
    public async Task CreateWalletRequest_ShouldCallCommandAndReturnSuccess(
        string? symbolicName)
    {
        // Arrange
        var userId = ""; //await CreateUser();
        var request = new CreateWalletRequest(symbolicName);

        // Act        
        var response = await _client.PostAsJsonAsync($"/api/users/{userId}/wallets", request);

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<CreateWalletResponse>();
        content.Should().NotBeNull();
        content?.WalletId.Should().NotBeNullOrEmpty();
    }
    
    #endregion
}
