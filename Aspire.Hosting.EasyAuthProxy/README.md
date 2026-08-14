# Aspire.Hosting.EasyAuthProxy

An Aspire hosting extension for [EasyAuth Dev Proxy](https://github.com/alanta/EasyAuthDevProxy), enabling easy local development of Azure Container Apps with EasyAuth authentication simulation.

## Installation

```xml
<PackageReference Include="Aspire.Hosting.EasyAuthProxy" Version="x.y.z" />
```

Or, for local development against this repo:

```xml
<ProjectReference Include="path/to/Aspire.Hosting.EasyAuthProxy.csproj" IsAspireProjectResource="false" />
```

## Usage

### Basic Setup

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var catalogService = builder.AddProject<Projects.CatalogService>("catalog");

// Add EasyAuth proxy with fluent configuration
var easyAuthProxy = builder.AddEasyAuthProxy("easyauth")
    .WithBackend(catalogService)
    .WithHostPort(8888);

builder.Build().Run();
```

### API Reference

#### `AddEasyAuthProxy(string name)`
Adds the EasyAuth development proxy as a plain .NET process. This is the default and recommended
way to add it: the proxy binaries are bundled inside this package, so it only needs a matching
.NET runtime on the host - no Docker or Podman required, and no image to pull.

#### `AddEasyAuthProxyContainer(string name)`
Adds the proxy as a container resource instead, pulled from `ghcr.io/alanta/easyauthdevproxy`.
Use this if you specifically want container-based isolation; it requires Docker or Podman.

#### `WithBackend<TResource>(IResourceBuilder<TResource> backend)`
Configures the proxy to forward requests to the specified backend service using Aspire's service discovery.
Works with both `AddEasyAuthProxy` and `AddEasyAuthProxyContainer`.

#### `WithHostPort<TResource>(int? port)`
Configures the host port for the proxy. Works with both `AddEasyAuthProxy` and `AddEasyAuthProxyContainer`.

## How it Works

1. **Service Discovery**: The proxy automatically discovers your backend services using Aspire's built-in service discovery
2. **Authentication Simulation**: Provides a login form where you can configure user identity and roles
3. **Header Injection**: Injects the appropriate EasyAuth headers into requests forwarded to your backend
4. **Seamless Integration**: Works with any backend that supports Azure Container Apps EasyAuth

## Requirements

- .NET 10.0 runtime (for `AddEasyAuthProxy`) - already true for anyone running an Aspire AppHost
- Aspire 9.4.0+
- Docker or Podman, only if you opt into `AddEasyAuthProxyContainer`

## Third-party notices

`AddEasyAuthProxy` bundles a published build of the EasyAuth proxy, including its third-party
dependencies (Yarp, OpenTelemetry, Azure.Core, etc.), so it can run without a separate download.
Those dependencies are redistributed under their original MIT / Apache-2.0 licenses - see
[THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt) for the full list and license texts.

## Contributing

This is part of the [EasyAuth Dev Proxy](https://github.com/alanta/EasyAuthDevProxy) project. Please see the main repository for contribution guidelines.
