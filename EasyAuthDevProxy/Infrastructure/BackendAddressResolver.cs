using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.ServiceDiscovery;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.ServiceDiscovery;

namespace EasyAuthDevProxy.Infrastructure;

/// <summary>
/// Resolve backend addresses using the service discovery service.
/// <para>This is copied from <see cref="Microsoft.Extensions.ServiceDiscovery.Yarp.ServiceDiscoveryDestinationResolver.cs"/> with a few modifications.</para>
/// </summary>
/// <param name="configuration">The configuration,</param>
/// <param name="resolver">The service endpoint resolver,</param>
/// <param name="options">Service discovery options.</param>
public class BackendAddressResolver(
    IConfiguration configuration,
    ServiceEndpointResolver resolver,
    IOptions<ServiceDiscoveryOptions> options)
    : IDestinationResolver
{
    private readonly ServiceDiscoveryOptions _options = options.Value;

    /// <inheritdoc/>
    public async ValueTask<ResolvedDestinationCollection> ResolveDestinationsAsync(IReadOnlyDictionary<string, DestinationConfig> destinations, CancellationToken cancellationToken)
    {
        var backendSetting = configuration.GetValue<string>("backend");

        if (string.IsNullOrEmpty(backendSetting)) throw new InvalidOperationException($"Setting {backendSetting} not provided");
        
        Dictionary<string, DestinationConfig> results = new();
        var tasks = new List<Task<(List<(string Name, DestinationConfig Config)>, IChangeToken ChangeToken)>>(destinations.Count);
        foreach (var (destinationId, destinationConfig) in destinations)
        {
            tasks.Add(ResolveHostAsync(destinationId, 
                // Custom: override the destination if it is named "backend"
                destinationId == "backend" ? backendSetting : null,  
                destinationConfig, 
                cancellationToken));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        var changeTokens = new List<IChangeToken>();
        foreach (var task in tasks)
        {
            var (configs, changeToken) = await task.ConfigureAwait(false);
            if (changeToken is not null)
            {
                changeTokens.Add(changeToken);
            }

            foreach (var (name, config) in configs)
            {
                results[name] = config;
            }
        }

        return new ResolvedDestinationCollection(results, new CompositeChangeToken(changeTokens));
    }

    private async Task<(List<(string Name, DestinationConfig Config)>, IChangeToken ChangeToken)> ResolveHostAsync(
        string originalName,
        string? serviceOverride,
        DestinationConfig originalConfig,
        CancellationToken cancellationToken)
    {
        var originalUri = new Uri(originalConfig.Address);
        
        if( serviceOverride is not null)
        {
            // Custom: apply override
            originalUri = serviceOverride.StartsWith("http") ? new Uri(serviceOverride) : new Uri($"http://{serviceOverride}");
        }
        var serviceName = originalUri.GetLeftPart(UriPartial.Authority);

        var result = await resolver.GetEndpointsAsync(serviceName, cancellationToken).ConfigureAwait(false);
        var results = new List<(string Name, DestinationConfig Config)>(result.Endpoints.Count);
        var uriBuilder = new UriBuilder(originalUri);
        var healthUri = originalConfig.Health is { Length: > 0 } health ? new Uri(health) : null;
        var healthUriBuilder = healthUri is { } ? new UriBuilder(healthUri) : null;
        foreach (var endpoint in result.Endpoints)
        {
            var addressString = endpoint.ToString()!;
            Uri uri;
            if (!addressString.Contains("://"))
            {
                var scheme = GetDefaultScheme(originalUri);
                uri = new Uri($"{scheme}://{addressString}");
            }
            else
            {
                uri = new Uri(addressString);
            }

            uriBuilder.Scheme = uri.Scheme;
            uriBuilder.Host = uri.Host;
            uriBuilder.Port = uri.Port;
            var resolvedAddress = uriBuilder.Uri.ToString();
            var healthAddress = originalConfig.Health;
            if (healthUriBuilder is not null)
            {
                healthUriBuilder.Host = uri.Host;
                healthUriBuilder.Port = uri.Port;
                healthAddress = healthUriBuilder.Uri.ToString();
            }

            var name = $"{originalName}[{addressString}]";
            string? resolvedHost;

            // Use the configured 'Host' value if it is provided.
            if (!string.IsNullOrEmpty(originalConfig.Host))
            {
                resolvedHost = originalConfig.Host;
            }
            else if (uri.IsLoopback)
            {
                // If there is no configured 'Host' value and the address resolves to localhost, do not set a host.
                // This is to account for non-wildcard development certificate.
                resolvedHost = null;
            }
            else
            {
                // Excerpt from RFC 9110 Section 7.2: The "Host" header field in a request provides the host and port information from the target URI [...]
                // See: https://www.rfc-editor.org/rfc/rfc9110.html#field.host
                // i.e, use Authority and not Host.
                resolvedHost = originalUri.Authority;
            }

            var config = originalConfig with { Host = resolvedHost, Address = resolvedAddress, Health = healthAddress };
            results.Add((name, config));
        }

        return (results, result.ChangeToken);
    }

    private string GetDefaultScheme(Uri originalUri)
    {
        if (originalUri.Scheme.IndexOf('+') > 0)
        {
            // Use the first allowed scheme.
            var specifiedSchemes = originalUri.Scheme.Split('+');
            foreach (var scheme in specifiedSchemes)
            {
                if (_options.AllowAllSchemes || _options.AllowedSchemes.Contains(scheme, StringComparer.OrdinalIgnoreCase))
                {
                    return scheme;
                }
            }

            throw new InvalidOperationException($"None of the specified schemes ('{string.Join(", ", specifiedSchemes)}') are allowed by configuration.");
        }
        else
        {
            return originalUri.Scheme;
        }
    }
}