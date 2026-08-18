using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Shouldly;

namespace Alanta.Aspire.Hosting.EasyAuthProxy.Tests;

public class When_adding_the_proxy
{
    [Fact]
    public void It_should_let_Aspire_allocate_the_target_ports_for_the_executable()
    {
        var builder = DistributedApplication.CreateBuilder();

        var proxy = builder.AddEasyAuthProxy("easyauth");

        // Issue #11: the proxy runs as a plain host process, so a pinned target port is the literal
        // OS port it binds and collides with anything else on the machine already holding it.
        foreach (var endpoint in proxy.Resource.Annotations.OfType<EndpointAnnotation>())
        {
            endpoint.TargetPort.ShouldBeNull(
                $"the target port of the {endpoint.Name} endpoint must be allocated by Aspire, not pinned");
        }
    }

    [Fact]
    public async Task It_should_hand_the_allocated_port_to_the_process_through_an_environment_variable()
    {
        var builder = DistributedApplication.CreateBuilder();

        var proxy = builder.AddEasyAuthProxy("easyauth");

        // Leaving the target port unpinned is only half of it - without a variable bound to the
        // endpoint, DCP has no way to pass the allocated port in and refuses to create the
        // endpoint at all ("information about the port to expose the service is missing").
        var environment = await proxy.ResolveEnvironmentAsync();

        environment["ASPNETCORE_HTTP_PORTS"].ShouldBe("{easyauth.bindings.http.targetPort}");
    }

    [Fact]
    public async Task It_should_not_pin_the_listen_address_itself()
    {
        var builder = DistributedApplication.CreateBuilder();

        var proxy = builder.AddEasyAuthProxy("easyauth");

        // Computing ASPNETCORE_URLS here cannot work: the target port is still unallocated when
        // environment callbacks run, and ASPNETCORE_URLS would override the port variables anyway.
        var environment = await proxy.ResolveEnvironmentAsync();

        environment.ShouldNotContainKey("ASPNETCORE_URLS");
    }

    [Fact]
    public void It_should_pin_the_container_target_ports()
    {
        var builder = DistributedApplication.CreateBuilder();

        var proxy = builder.AddEasyAuthProxyContainer("easyauth");

        // The opposite of the executable case: this is a container-internal port, remapped to a
        // free host port by the container runtime, so pinning it is safe.
        var endpoints = proxy.Resource.Annotations.OfType<EndpointAnnotation>().ToDictionary(e => e.Name);

        endpoints["http"].TargetPort.ShouldBe(8080);
    }
}
