// =============================================================================
// Endpoint Integration Tests — Testing API endpoints with real database
// =============================================================================
// These tests demonstrate the correct patterns for integration testing
// ASP.NET Core Minimal API endpoints backed by PostgreSQL via Testcontainers.
//
// Key conventions:
//   - Naming: MethodName_Scenario_ExpectedResult
//   - NO "Arrange", "Act", "Assert" comments — structure is self-evident
//   - FluentAssertions for all assertions
//   - Primary constructor for fixture injection
//   - One logical assertion group per test
// =============================================================================

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PointsWallet.Api.Endpoints;
using PointsWallet.Api.Requests.Users;
using PointsWallet.Api.Requests.Wallets;
using PointsWallet.IntegrationTests.Fixtures;

namespace PointsWallet.IntegrationTests.Api.Users;

// ---------------------------------------------------------------------------
// User Endpoint Tests
// ---------------------------------------------------------------------------
// Uses IClassFixture to share the PostgreSQL container across all tests.
// The fixture provides a pre-configured HttpClient that routes to the
// in-memory test server with real database and bypassed authentication.
// ---------------------------------------------------------------------------

public class UserEndpointsTests(PointsWalletWebApplicationFixture fixture)
    : IClassFixture<PointsWalletWebApplicationFixture>
{
    private readonly HttpClient _client = fixture.Client;

    [Fact]
    public async Task CreateUser_WithValidRequest_ReturnsCreatedWithUserId()
    {
        var request = new CreateUserRequest("John Doe", "john.doe@example.com");

        var response = await _client.PostAsJsonAsync("/api/users/", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content
            .ReadFromJsonAsync<CreateUserResponse>();
        content.Should().NotBeNull();
        content!.UserId.Should().NotBeNullOrEmpty();
    }
}

namespace PointsWallet.IntegrationTests.Api.Wallets;

// ---------------------------------------------------------------------------
// Wallet Endpoint Tests
// ---------------------------------------------------------------------------
// Demonstrates Theory/InlineData for parameterized integration tests.
// Each InlineData variant runs the full pipeline: API → MediatR → EF Core → DB.
// ---------------------------------------------------------------------------

public class WalletEndpointsTests(PointsWalletWebApplicationFixture fixture)
    : IClassFixture<PointsWalletWebApplicationFixture>
{
    private readonly HttpClient _client = fixture.Client;

    [Theory]
    [InlineData("My Wallet")]
    [InlineData(null)]
    public async Task CreateWallet_WithValidRequest_ReturnsCreatedWithWalletId(
        string? symbolicName)
    {
        var userId = Guid.NewGuid().ToString();
        var request = new CreateWalletRequest(symbolicName);

        var response = await _client.PostAsJsonAsync(
            $"/api/users/{userId}/wallets", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content
            .ReadFromJsonAsync<CreateWalletResponse>();
        content.Should().NotBeNull();
        content!.WalletId.Should().NotBeNullOrEmpty();
    }
}
