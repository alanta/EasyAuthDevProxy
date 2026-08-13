using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Represents an EasyAuth development proxy resource running as a container.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class EasyAuthProxyContainerResource(string name) : ContainerResource(name), IResourceWithServiceDiscovery
{
    /// <summary>
    /// Gets or sets the backend service that this proxy forwards requests to.
    /// </summary>
    public IResourceWithEndpoints? Backend { get; set; }
}