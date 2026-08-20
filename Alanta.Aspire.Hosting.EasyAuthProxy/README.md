# Alanta.Aspire.Hosting.EasyAuthProxy

An Aspire hosting extension for [EasyAuth Dev Proxy](https://github.com/alanta/EasyAuthDevProxy), enabling easy local development of Azure Container Apps with EasyAuth authentication simulation.

## Installation

```xml
<PackageReference Include="Alanta.Aspire.Hosting.EasyAuthProxy" Version="x.y.z" />
```

Or, for local development against this repo:

```xml
<ProjectReference Include="path/to/Alanta.Aspire.Hosting.EasyAuthProxy.csproj" IsAspireProjectResource="false" />
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
Configures the host port for the proxy's HTTP endpoint. Works with both `AddEasyAuthProxy` and
`AddEasyAuthProxyContainer`. Leave it unset and Aspire assigns a free port.

#### `WithHttpsHostPort<TResource>(int? port)`
Same, for the proxy's HTTPS endpoint.

### HTTPS

Both variants expose an `https` endpoint next to the `http` one, so a backend that only makes sense
over TLS (secure cookies, HSTS, `RequireHttpsMetadata`) can be reached the same way it would be in
Container Apps:

```csharp
var easyAuthProxy = builder.AddEasyAuthProxy("easyauth")
    .WithBackend(catalogService)
    .WithHostPort(8888)
    .WithHttpsHostPort(8889);
```

By default the proxy serves the **ASP.NET Core developer certificate**, so make sure it's trusted:

```shell
dotnet dev-certs https --trust
```

To serve a certificate of your own, use Aspire's standard certificate extensions - the proxy picks
up whatever they configure and passes it to Kestrel:

```csharp
var certPassword = builder.AddParameter("cert-password", secret: true);
var certificate = X509CertificateLoader.LoadPkcs12FromFile("certs/easyauth.pfx", "...");

var easyAuthProxy = builder.AddEasyAuthProxy("easyauth")
    .WithBackend(catalogService)
    .WithHttpsCertificate(certificate, certPassword);
```

> ℹ️ `WithHttpsCertificate` / `WithHttpsDeveloperCertificate` are still marked experimental in
> Aspire 13.5, so calling them from your AppHost needs
> `#pragma warning disable ASPIRECERTIFICATES001` (or `<NoWarn>ASPIRECERTIFICATES001</NoWarn>`).
> Nothing extra is needed if you stick with the default developer certificate.

### Backends over HTTPS

When your backend serves HTTPS the proxy has to validate its certificate, and the developer
certificate usually fails that: the name doesn't match once the backend is reached through service
discovery, and on Linux `dotnet dev-certs https --trust` only partially succeeds, so the chain
isn't trusted either. The proxy therefore accepts any certificate the backend presents when
`ASPNETCORE_ENVIRONMENT` is `Development` - it logs a warning at startup saying so.

This means the backend is **not** authenticated. That's the right trade for local development, but
if you're pointing the proxy at something you don't fully control, turn it off and make sure the
backend serves a certificate that validates:

```csharp
var easyAuthProxy = builder.AddEasyAuthProxy("easyauth")
    .WithBackend(catalogService)
    .WithEnvironment("EasyAuth__AllowUntrustedBackendCertificate", "false");
```

Outside `Development` the setting defaults to `false` and has to be switched on deliberately.

## How it Works

1. **Service Discovery**: The proxy automatically discovers your backend services using Aspire's built-in service discovery
2. **Authentication Simulation**: Provides a login form where you can configure user identity and roles
3. **Header Injection**: Injects the appropriate EasyAuth headers into requests forwarded to your backend
4. **Seamless Integration**: Works with any backend that supports Azure Container Apps EasyAuth

## Requirements

- .NET 10.0 runtime (for `AddEasyAuthProxy`) - already true for anyone running an Aspire AppHost
- Aspire 13.5.0+
- Docker or Podman, only if you opt into `AddEasyAuthProxyContainer`

## Third-party notices

`AddEasyAuthProxy` bundles a published build of the EasyAuth proxy, including its third-party
dependencies (Yarp, OpenTelemetry, Azure.Core, etc.), so it can run without a separate download.
Those dependencies are redistributed under their original MIT / Apache-2.0 licenses - see
[THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt) for the full list and license texts.

## Contributing

This is part of the [EasyAuth Dev Proxy](https://github.com/alanta/EasyAuthDevProxy) project. Please see the main repository for contribution guidelines.
