# EasyAuth Dev Proxy for Azure Container Apps

Run your container app with EasyAuth enabled in local development.

## About this project

This project was created to scratch an itch: I wanted to run my [Azure Container App](https://learn.microsoft.com/en-us/azure/container-apps/overview) locally with [EasyAuth](https://learn.microsoft.com/en-us/azure/container-apps/authentication) enabled.

It's a [YARP](https://microsoft.github.io/reverse-proxy/) based reverse proxy that intercepts the EasyAuth endpoints to allows logging in locally.

### Features

* Run your container app with EasyAuth enabled in local development
* Simulate login, similar to what [SWA CLI](https://azure.github.io/static-web-apps-cli/) enables for Azure Static WebApps
* Run your app in a container or `dotnet run` it (or whatever platform your app runs in)
* No need to change your app, just point the proxy to your backend

### Limitations

* Credentials are faked and not backed by any identity provider.
* Only the bare minimum of claims is added to the client identity: username, roles, id, provider.
* Haven't figured out single-click launch yet, so you need to run it from the command line.
* Assumes your app allows anonymous access and redirect to login when needed.

## Usage

### Running from source

1. Make sure you have dotnet 8 installed.

2. Clone this repo.

3. Start the proxy with the following command in the `EasyAuthDevProxy` folder:

  ```pwsh
  dotnet run --urls=https://localhost:8888 --backend=https://localhost:7290
  ```
  
  The `urls` parameter is the frontend URL of the proxy that you'll point your browser to.
  The `backend` parameter is the URL of your backend app.
  
4. Open your browser and navigate to the proxy URL, e.g. `https://localhost:8888` in the example above.

When your application redirects to the login page (for example `/.auth/login/aad`), you'll be presented with a form that allows you to configure the user and roles.

### Docker

The latest version of this project is available as a public container on GitHub Container Registry.

The following command will run the EasyAuth Dev Proxy on `http://localhost:8888` with the backend url set to `http://localhost:5191`.

```shell
docker run --network=host -d --rm ghcr.io/alanta/easyauthdevproxy:latest -e backend=http://localhost:5191 -p 8080:8888
```

> ⚠️ This setup does not support HTTPS because there is no TLS certificate included in the container.

## Credits

* John Reilly for sharing his code to [enable EasyAuth in dotnet container apps](https://johnnyreilly.com/azure-container-apps-easy-auth-and-dotnet-authentication).
