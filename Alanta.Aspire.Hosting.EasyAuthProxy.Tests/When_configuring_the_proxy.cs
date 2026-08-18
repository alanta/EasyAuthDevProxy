using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Shouldly;

namespace Alanta.Aspire.Hosting.EasyAuthProxy.Tests;

public class When_configuring_the_proxy
{
    [Fact]
    public void WithHostPort_should_change_the_advertised_port_only()
    {
        var builder = DistributedApplication.CreateBuilder();

        var proxy = builder.AddEasyAuthProxy("easyauth").WithHostPort(8888);

        var http = proxy.Resource.Annotations.OfType<EndpointAnnotation>().Single(e => e.Name == "http");

        http.Port.ShouldBe(8888);
        http.TargetPort.ShouldBeNull("the port the process actually binds stays Aspire's to allocate");
    }

    [Fact]
    public void WithHttpsHostPort_should_change_the_advertised_https_port_only()
    {
        var builder = DistributedApplication.CreateBuilder();

        var proxy = builder.AddEasyAuthProxy("easyauth").WithHttpsHostPort(8889);

        var https = proxy.Resource.Annotations.OfType<EndpointAnnotation>().Single(e => e.Name == "https");

        https.Port.ShouldBe(8889);
        https.TargetPort.ShouldBeNull();
    }

    [Fact]
    public async Task WithBackend_should_point_the_proxy_at_the_backend_through_service_discovery()
    {
        var builder = DistributedApplication.CreateBuilder();
        var backend = builder.AddResource(new TestBackend("catalog"))
            .WithHttpEndpoint(name: "http", targetPort: 8080)
            .WithHttpsEndpoint(name: "https", targetPort: 8081);

        var proxy = builder.AddEasyAuthProxy("easyauth").WithBackend(backend);

        var environment = await proxy.ResolveEnvironmentAsync();

        environment["BACKEND"].ShouldBe("https+http://catalog");
    }

    /// <summary>Stand-in for any backend a consumer would point the proxy at.</summary>
    private sealed class TestBackend(string name) : Resource(name), IResourceWithServiceDiscovery;
}
