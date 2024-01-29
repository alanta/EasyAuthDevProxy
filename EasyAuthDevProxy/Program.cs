using EasyAuthDevProxy.Infrastructure;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddRazorPages();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddConfigFilter<MyConfigFilter>()
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(EasyAuth.EasyAuthTransform);
    });

var app = builder.Build();

app.UseStaticFiles("/.auth/assets");

app.UseRouting();

app.MapGet("/health", () => "OK");
app.MapGet("/.auth/logout", EasyAuth.Logout);

app.MapRazorPages();

app.MapReverseProxy();

app.Run();



// This is to enable tests to attach to the program
public partial class Program { }

public class MyConfigFilter(IConfiguration configuration) : IProxyConfigFilter
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