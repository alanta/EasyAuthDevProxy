using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Represents an EasyAuth development proxy resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class EasyAuthProxyResource(string name) : Resource(name), IResourceWithServiceDiscovery, IResourceWithEnvironment
{
    /// <summary>
    /// Gets the primary endpoint for the EasyAuth proxy.
    /// </summary>
    public EndpointReference PrimaryEndpoint => new(this, "https");

    /// <summary>
    /// Gets or sets the backend service that this proxy forwards requests to.
    /// </summary>
    public IResourceWithEndpoints? Backend { get; set; }

    /// <summary>
    /// Gets or sets the default username for authentication simulation.
    /// </summary>
    public string? DefaultUsername { get; set; }

    /// <summary>
    /// Gets or sets the default roles for authentication simulation.
    /// </summary>
    public string[]? DefaultRoles { get; set; }

    /// <summary>
    /// Gets or sets the identity provider name.
    /// </summary>
    public string IdentityProvider { get; set; } = "aad";
}
