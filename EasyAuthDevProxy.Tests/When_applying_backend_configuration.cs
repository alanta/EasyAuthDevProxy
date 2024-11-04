using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ServiceDiscovery;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.ServiceDiscovery;

namespace EasyAuthDevProxy.Tests;

public class When_applying_backend_configuration  : IClassFixture<EasyAuthDevProxyApplicationFactory>
{
    [Theory,
     InlineData("webfrontend", "http://localhost:5612/", "Default to http when no scheme is provided and resolve the endpoint"),
     InlineData("http://webfrontend", "http://localhost:5612/", "Use provided http scheme and resolve the endpoint"),
     InlineData("http://webfrontend/api/", "http://localhost:5612/api/", "Use provided http scheme, resolve the endpoint and keep the path"),
     InlineData("https://webfrontend", "https://localhost:5613/", "Use provided https scheme and resolve the endpoint"),
     InlineData("https+http://webfrontend", "https://localhost:5613/", "Use provided preferred https scheme and resolve the endpoint"),
     InlineData("http://somewhere.local:1756", "http://somewhere.local:1756/", "Service resolution is unavailable, use the provided url"),
     InlineData("whatever/api", "http://whatever/api", "Service resolution is unavailable, use the provided url"),]
    public async Task It_should_use_service_discovery_to_resolve_the_endpoint(string backend, string expectedUrl, string because)
    {
        // Arrange
        var server = new EasyAuthDevProxyApplicationFactory()
            .WithSetting("backend", backend)
            .WithSetting("services:webfrontend:http:0", "http://localhost:5612")
            .WithSetting("services:webfrontend:https:0", "https://localhost:5613");
        
        using var scope = server.Services.CreateScope();

        var sut = scope.ServiceProvider.GetRequiredService<IDestinationResolver>();
        var config = new DestinationConfig()
        {
            Address = "http://localhost:5000",
        };
        
        // Act
        var result = await sut.ResolveDestinationsAsync(
            new Dictionary<string, DestinationConfig>([new ("backend", config)])
            , CancellationToken.None);

        // Assert
        result.Destinations.Should().HaveCount(1);
        result.Destinations.First().Value.Address.Should().Be(expectedUrl, because);
    }
}

