# Aspire.Hosting.EasyAuthProxy

An Aspire hosting extension for [EasyAuth Dev Proxy](https://github.com/alanta/EasyAuthDevProxy), enabling easy local development of Azure Container Apps with EasyAuth authentication simulation.

## Installation

Add the project reference to your Aspire AppHost project:

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
    .WithDefaultUser("developer@contoso.com", "Admin", "User")
    .WithIdentityProvider("aad")
    .WithHostPort(8888);

builder.Build().Run();
```

### API Reference

#### `AddEasyAuthProxy(string name)`
Adds an EasyAuth development proxy as a container resource.

#### `WithBackend<T>(IResourceBuilder<T> backend)`
Configures the proxy to forward requests to the specified backend service using Aspire's service discovery.

#### `WithDefaultUser(string username, params string[] roles)`
Configures default user credentials for authentication simulation. These values will be pre-filled in the login form.

#### `WithIdentityProvider(string provider)`
Sets the identity provider name (e.g., "aad", "facebook", "google").

#### `WithHostPort(int port)`
Configures the host port for the proxy (default: 8888).

## How it Works

1. **Service Discovery**: The proxy automatically discovers your backend services using Aspire's built-in service discovery
2. **Authentication Simulation**: Provides a login form where you can configure user identity and roles
3. **Header Injection**: Injects the appropriate EasyAuth headers into requests forwarded to your backend
4. **Seamless Integration**: Works with any backend that supports Azure Container Apps EasyAuth

## Development

The proxy runs as a container by default, but for debugging purposes, you can run it as a project by modifying the extension or using the original project-based approach.

## Requirements

- .NET 9.0
- Aspire 9.4.0+
- Docker (for container-based execution)

## Contributing

This is part of the [EasyAuth Dev Proxy](https://github.com/alanta/EasyAuthDevProxy) project. Please see the main repository for contribution guidelines.
