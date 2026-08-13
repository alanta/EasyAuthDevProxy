using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for configuring EasyAuth proxy resources.
/// </summary>
public static class EasyAuthProxyResourceBuilderExtensions
{
    /// <summary>
    /// Configures the EasyAuth proxy to forward requests to the specified backend service.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="backend">The backend service to proxy requests to.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{EasyAuthProxyContainerResource}"/>.</returns>
    public static IResourceBuilder<EasyAuthProxyContainerResource> WithBackend(
        this IResourceBuilder<EasyAuthProxyContainerResource> builder,
        IResourceBuilder<IResourceWithServiceDiscovery> backend)
    {
        builder.WithReference(backend);
        return builder.WithEnvironment(context =>
        {
            context.EnvironmentVariables["BACKEND"] = BuildEndpointUri(backend.Resource);
        });
    }

    /// <summary>
    /// Configures the host port that the YARP resource is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder for YARP.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used random port will be assigned.</param>
    public static IResourceBuilder<EasyAuthProxyContainerResource> WithHostPort(this IResourceBuilder<EasyAuthProxyContainerResource> builder, int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Adds an EasyAuth development proxy resource, running as a container pulled from
    /// <c>ghcr.io/alanta/easyauthdevproxy</c>.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{EasyAuthProxyContainerResource}"/>.</returns>
    public static IResourceBuilder<EasyAuthProxyContainerResource> AddEasyAuthProxy(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new EasyAuthProxyContainerResource(name);
        var yarpBuilder = builder.AddResource(resource)
            .WithHttpEndpoint(name: "http", targetPort: 8080)
            .WithImage("alanta/easyauthdevproxy")
            .WithImageRegistry("ghcr.io")
            .WithImageTag("latest")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
            .WithContainerRuntimeArgs("--add-host=host.docker.internal:10.88.0.1")
            .WithEnvironment(ctx =>
            {
                // Patchup for podman
                foreach (var env in ctx.EnvironmentVariables.Where(e => e.Value is string sValue && sValue.Contains("host.docker.internal")).ToArray())
                {
                    ctx.EnvironmentVariables[env.Key] = (env.Value as string)!.Replace("host.docker.internal", "host.containers.internal");
                }
            })
            .WithOtlpExporter();


        if (builder.ExecutionContext.IsRunMode)
        {
            // YARP will not trust the cert used by Aspire otlp endpoint when running locally
            // The Aspire otlp endpoint uses the dev cert, only valid for localhost, but from the container
            // perspective, the url will be something like https://docker.host.internal, so it will NOT be valid.
            yarpBuilder.WithEnvironment("YARP_UNSAFE_OLTP_CERT_ACCEPT_ANY_SERVER_CERTIFICATE", "true");
        }
        
        return yarpBuilder;
    }
    
    private static string BuildEndpointUri(IResourceWithServiceDiscovery resource)
    {
        var resourceName = resource.Name;

        // NOTE: This should likely fallback to other endpoints with HTTP or HTTPS schemes in cases where they don't
        //       have the default names.
        var httpsEndpoint = resource.GetEndpoint("https");
        var httpEndpoint = resource.GetEndpoint("http");

        var scheme = (httpsEndpoint.Exists, httpEndpoint.Exists) switch
        {
            (true, true) => "https+http",
            (true, false) => "https",
            (false, true) => "http",
            _ => throw new ArgumentException("Cannot find a http or https endpoint for this resource.", nameof(resource))
        };

        return $"{scheme}://{resourceName}";
    }
}
