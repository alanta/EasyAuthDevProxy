using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Common contract shared by the executable- and container-based EasyAuth proxy resources, so
/// <c>WithBackend</c>/<c>WithHostPort</c> work regardless of which one is in use.
/// </summary>
public interface IEasyAuthProxyResource : IResourceWithServiceDiscovery, IResourceWithEndpoints, IResourceWithEnvironment;

/// <summary>
/// Represents the EasyAuth development proxy running as a plain .NET process. This is the
/// default: it only requires a matching .NET runtime, not a container engine.
/// </summary>
public class EasyAuthProxyExecutableResource(string name, string command, string workingDirectory)
    : ExecutableResource(name, command, workingDirectory), IEasyAuthProxyResource;

/// <summary>
/// Represents the EasyAuth development proxy running as a container pulled from
/// <c>ghcr.io/alanta/easyauthdevproxy</c>.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class EasyAuthProxyContainerResource(string name) : ContainerResource(name), IEasyAuthProxyResource;
