using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Alanta.Aspire.Hosting.EasyAuthProxy.Tests;

internal static class ResourceEnvironment
{
    /// <summary>
    /// Resolves a resource's environment variables without running the application.
    /// </summary>
    /// <remarks>
    /// Publish-mode resolution is what keeps this instant and is not optional: in run mode the
    /// value providers wait for endpoints DCP would have allocated, which never happens in a unit
    /// test, so the call blocks forever. Values referring to an endpoint therefore come back as
    /// placeholders such as <c>{easyauth.bindings.http.targetPort}</c> rather than real ports.
    /// </remarks>
    public static async Task<IReadOnlyDictionary<string, string>> ResolveEnvironmentAsync<TResource>(
        this IResourceBuilder<TResource> resource)
        where TResource : IResource
    {
        var executionContext = new DistributedApplicationExecutionContext(
            new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Publish)
            {
                ServiceProvider = resource.ApplicationBuilder.Services.BuildServiceProvider()
            });

        var configuration = await ExecutionConfigurationBuilder.Create(resource.Resource)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(executionContext);

        return configuration.EnvironmentVariables.ToDictionary(e => e.Key, e => e.Value);
    }
}
