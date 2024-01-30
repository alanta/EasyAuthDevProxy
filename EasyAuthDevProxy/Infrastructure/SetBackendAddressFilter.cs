using Yarp.ReverseProxy.Configuration;

namespace EasyAuthDevProxy.Infrastructure;

/// <summary>
/// Replaces the address for the backend cluster with the value from the "backend" setting.
/// The setting can be specified on the commandline, in appsettings.json or as an environment variable.
/// </summary>
/// <param name="configuration">The configuration for the application.</param>
public class SetBackendAddressFilter(IConfiguration configuration) : IProxyConfigFilter
{
    public ValueTask<ClusterConfig> ConfigureClusterAsync(ClusterConfig cluster, CancellationToken cancel)
    {
        if (cluster.ClusterId != "backend") return ValueTask.FromResult(cluster);

        var backendAddress = configuration.GetValue<string>("backend");

        if (string.IsNullOrEmpty(backendAddress)) throw new InvalidOperationException("backend address not configured");

        Console.WriteLine($"Backend address: {backendAddress}");

        var newDestinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
        {
            { "backend", new DestinationConfig{ Address = backendAddress} }
        };

        return ValueTask.FromResult( cluster with { Destinations = newDestinations } );

    }

    public ValueTask<RouteConfig> ConfigureRouteAsync(RouteConfig route, ClusterConfig? cluster, CancellationToken cancel)
    {
        return ValueTask.FromResult(route);
    }
}