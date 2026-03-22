using System.Net.Http.Json;
using FluentAssertions;
using PointsWallet.Api.Endpoints;
using PointsWallet.Api.Requests.Users;
using PointsWallet.IntegrationTests.Fixtures;

namespace PointsWallet.IntegrationTests.Api.Users;

public class UserEndpointsTests(PointsWalletWebApplicationFixture fixture) 
    : IClassFixture<PointsWalletWebApplicationFixture>
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task CreateUserAsync_ShouldCallCommandAndReturnSuccess()
    {
        // Arrange
        var request = new CreateUserRequest("John Doe", "john.doe@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/users/", request);

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
    }
}
