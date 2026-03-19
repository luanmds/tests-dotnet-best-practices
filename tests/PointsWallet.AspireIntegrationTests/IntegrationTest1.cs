using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using PointsWallet.AspireIntegrationTests.Fixtures;

namespace PointsWallet.AspireIntegrationTests;

[Collection(nameof(AspireAppCollection))]
public class IntegrationTest1(AspireAppFixture fixture)
{
    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {        
        // Act
        var response = await fixture.ApiClient
            .GetAsync("/api/users", AspireAppFixture.CancellationToken);
    
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
