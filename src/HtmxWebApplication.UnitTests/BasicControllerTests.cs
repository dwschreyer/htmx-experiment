using Microsoft.AspNetCore.Mvc.Testing;

namespace HtmxWebApplication.UnitTests;

public class BasicControllerTests(
    WebApplicationFactory<Program> factory) 
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task GetVersion_ReturnsSuccessAndCorrectContent()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/Basic/Version");

        // Assert
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        Assert.Contains("1.0.0", responseString);
    }
}
