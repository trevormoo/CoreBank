using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace CoreBank.Api.Tests.Controllers;

public class HealthControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturn200Ok()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/health");
        var content = await response.Content.ReadFromJsonAsync<HealthResponse>();

        // Assert
        content.Should().NotBeNull();
        content!.Status.Should().Be("Healthy");
        content.Version.Should().NotBeNullOrEmpty();
    }

    private class HealthResponse
    {
        public string Status { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public string Version { get; set; } = null!;
    }
}
