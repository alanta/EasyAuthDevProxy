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
    /// <returns>A reference to the <see cref="IResourceBuilder{ExecutableResource}"/>.</returns>
    public static IResourceBuilder<ExecutableResource> WithBackend<TBackend>(this IResourceBuilder<ExecutableResource> builder, IResourceBuilder<TBackend> backend)
        where TBackend : IResourceWithEndpoints
    {
        return builder.WithBackend(backend.Resource);
    }

    /// <summary>
    /// Configures the EasyAuth proxy to forward requests to the specified backend service.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="backend">The backend service to proxy requests to.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{ExecutableResource}"/>.</returns>
    public static IResourceBuilder<ExecutableResource> WithBackend(this IResourceBuilder<ExecutableResource> builder, IResourceWithEndpoints backend)
    {
        // For project-to-project communication, resolve the actual URL of the backend service
        return builder.WithEnvironment(context =>
        {
            var backendUrl = backend.GetEndpoint("https").Url;
            context.EnvironmentVariables["BACKEND"] = backendUrl;
        });
    }

    /// <summary>
    /// Adds an EasyAuth development proxy from the NuGet package as an executable resource.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="packagePath">The path to the NuGet package installation (optional, will use default if not provided).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{ExecutableResource}"/>.</returns>
    public static IResourceBuilder<ExecutableResource> AddEasyAuthProxyExecutable(this IDistributedApplicationBuilder builder, string name, string? packagePath = null)
    {
        var defaultPackagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
            ".nuget", "packages", "alanta.easyauthdevproxy", "1.0.0", "tools");
        
        var workingDirectory = packagePath ?? defaultPackagePath;

        // Use the self-contained executable directly
        var executablePath = Path.Combine(workingDirectory, "EasyAuthDevProxy");
        var easyAuthProxy = builder.AddExecutable(name, executablePath, workingDirectory, [])
                                .WithHttpEndpoint(port: 8888, name: "proxy")
                                .WithEnvironment("ASPNETCORE_URLS", "http://localhost:8888");
        return easyAuthProxy;
    }
}
