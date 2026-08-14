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
    public static IResourceBuilder<TResource> WithBackend<TResource>(
        this IResourceBuilder<TResource> builder,
        IResourceBuilder<IResourceWithServiceDiscovery> backend)
        where TResource : IEasyAuthProxyResource
    {
        builder.WithReference(backend);
        return builder.WithEnvironment(context =>
        {
            context.EnvironmentVariables["BACKEND"] = BuildEndpointUri(backend.Resource);
        });
    }

    /// <summary>
    /// Configures the host port that the proxy is exposed on instead of using a randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder for the EasyAuth proxy.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> a random port will be assigned.</param>
    public static IResourceBuilder<TResource> WithHostPort<TResource>(this IResourceBuilder<TResource> builder, int? port)
        where TResource : IEasyAuthProxyResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Adds an EasyAuth development proxy resource running as a plain .NET process. This is the
    /// default and recommended way to add the proxy: it only requires a matching .NET runtime,
    /// not a container engine. The proxy binaries are bundled inside this package.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource.</param>
    public static IResourceBuilder<EasyAuthProxyExecutableResource> AddEasyAuthProxy(this IDistributedApplicationBuilder builder, string name)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var proxyDllPath = ResolveProxyDllPath();
        var workingDirectory = Path.GetDirectoryName(proxyDllPath)!;

        var resource = new EasyAuthProxyExecutableResource(name, "dotnet", workingDirectory);
        var resourceBuilder = builder.AddResource(resource)
            .WithArgs(proxyDllPath)
            .WithHttpEndpoint(name: "http", targetPort: 8080)
            .WithEnvironment(context =>
            {
                // Unlike project resources, plain executables don't get ASPNETCORE_URLS
                // populated automatically from the endpoint - and WithHttpEndpoint's own
                // `env:` shortcut only injects the bare port, which Kestrel rejects. Bind the
                // full URL ourselves instead.
                var endpoint = resource.GetEndpoint("http");
                context.EnvironmentVariables["ASPNETCORE_URLS"] = $"http://+:{endpoint.TargetPort}";
            })
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
            .WithOtlpExporter();

        return resourceBuilder;
    }

    /// <summary>
    /// Adds an EasyAuth development proxy resource, running as a container pulled from
    /// <c>ghcr.io/alanta/easyauthdevproxy</c>. Prefer <see cref="AddEasyAuthProxy"/> unless you
    /// specifically need container-based isolation - this path requires Docker or Podman and
    /// pulls a full container image.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource.</param>
    public static IResourceBuilder<EasyAuthProxyContainerResource> AddEasyAuthProxyContainer(this IDistributedApplicationBuilder builder, string name)
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

    private static string ResolveProxyDllPath()
    {
        var assemblyDirectory = Path.GetDirectoryName(typeof(EasyAuthProxyResourceBuilderExtensions).Assembly.Location)!;

        // In-repo / ProjectReference build: the proxy's publish output is copied right next to
        // this assembly on every build (see Alanta.Aspire.Hosting.EasyAuthProxy.csproj).
        var sibling = Path.Combine(assemblyDirectory, "proxy", "EasyAuthDevProxy.dll");
        if (File.Exists(sibling))
        {
            return sibling;
        }

        // Packed NuGet layout: this assembly lives under lib/<tfm>/, the proxy output is packed
        // at the package root under proxy/.
        var packaged = Path.GetFullPath(Path.Combine(assemblyDirectory, "..", "..", "proxy", "EasyAuthDevProxy.dll"));
        if (File.Exists(packaged))
        {
            return packaged;
        }

        throw new FileNotFoundException(
            "Could not locate the bundled EasyAuthDevProxy.dll next to the Alanta.Aspire.Hosting.EasyAuthProxy assembly. " +
            $"Looked in '{sibling}' and '{packaged}'.");
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
