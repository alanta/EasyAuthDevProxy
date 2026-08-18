# EasyAuth Dev Proxy for Azure Container Apps

Run your container app with EasyAuth enabled in local development.

## About this project

This project was created to scratch an itch: I wanted to run my [Azure Container App](https://learn.microsoft.com/en-us/azure/container-apps/overview) locally with [EasyAuth](https://learn.microsoft.com/en-us/azure/container-apps/authentication) enabled.

It's a [YARP](https://microsoft.github.io/reverse-proxy/) based reverse proxy that intercepts the EasyAuth endpoints to allow logging in locally.

Read more about the motivation behind this project in the [launch blog post](https://alanta.nl/posts/2024/02/dev-proxy-for-easy-auth-on-container-apps), and about the Aspire integration in [EasyAuth Dev Proxy, now with Aspire](https://alanta.nl/posts/2026/08/easyauth-dev-proxy-aspire-integration).

### Features

* **Easy Aspire Integration** - Fluent API for seamless integration with .NET Aspire applications
* Run your container app with EasyAuth enabled in local development
* Simulate login, similar to what [SWA CLI](https://azure.github.io/static-web-apps-cli/) enables for Azure Static WebApps
* Run your app in a container or `dotnet run` it (or whatever platform your app runs in)
* No need to change your app, just point the proxy to your backend
* Support for Service Discovery to enable easy integration with Aspire

### Limitations

* Credentials are faked and not backed by any identity provider.
* Only the bare minimum of claims is added to the client identity: username, roles, id, provider.
* Assumes your app allows anonymous access and redirect to login when needed.

### What's New

* ✅ **Single-click launch** - Now available through Aspire integration!
* ✅ **Automatic service discovery** - No need to manually configure backend URLs
* ✅ **Fluent configuration API** - Easy to set up and customize
* ✅ **HTTPS support** - Use the development certificate or provide your own

## Usage

### Aspire Integration (Recommended)

The easiest way to use EasyAuth Dev Proxy is through the [`Alanta.Aspire.Hosting.EasyAuthProxy`](https://www.nuget.org/packages/Alanta.Aspire.Hosting.EasyAuthProxy) NuGet package, which adds a fluent API to your Aspire AppHost. See the [package README](Alanta.Aspire.Hosting.EasyAuthProxy/README.md) for usage instructions.

### Running from source

1. Make sure you have .NET 10 installed.

2. Clone this repo.

3. Start the proxy with the following command in the `EasyAuthDevProxy` folder:

  ```pwsh
  dotnet run --urls=https://localhost:8888 --backend=https://localhost:7290
  ```
  
  The `urls` parameter is the frontend URL of the proxy that you'll point your browser to.
  The `backend` parameter is the URL of your backend app.

  If your backend serves HTTPS with the developer certificate, note that the proxy accepts any
  certificate the backend presents while `ASPNETCORE_ENVIRONMENT` is `Development` - the developer
  certificate otherwise fails validation on both the host name and the chain. Set
  `EasyAuth__AllowUntrustedBackendCertificate=false` to require a valid certificate instead; it
  already defaults to `false` outside `Development`.
  
4. Open your browser and navigate to the proxy URL, e.g. `https://localhost:8888` in the example above.

When your application redirects to the login page (for example `/.auth/login/aad`), you'll be presented with a form that allows you to configure the user and roles.

### Docker

The latest version of this project is available as a public container on GitHub Container Registry.

The following command will run the EasyAuth Dev Proxy on `http://localhost:8888` with the backend url set to `http://localhost:5191`.

```shell
docker run --network=host -d --rm -e backend=http://localhost:5191 -e ASPNETCORE_HTTP_PORTS=8888 ghcr.io/alanta/easyauthdevproxy:latest
```

`--network=host` is what lets the container reach a backend listening on the host's `localhost`.
The proxy listens on port 8080 by default; `ASPNETCORE_HTTP_PORTS` moves it.

> ⚠️ Run this way, the proxy is HTTP-only - there is no TLS certificate inside the container.
> The Aspire integration does serve HTTPS: it hands the container the ASP.NET Core developer
> certificate (or one you supply). See the [package README](Alanta.Aspire.Hosting.EasyAuthProxy/README.md#https).

## Credits

* John Reilly for sharing his code to [enable EasyAuth in dotnet container apps](https://johnnyreilly.com/azure-container-apps-easy-auth-and-dotnet-authentication).
