using Yarp.ReverseProxy.ServiceDiscovery;

namespace EasyAuthDevProxy.Infrastructure;

public static class ReverseProxyConfigExtensions
{
    public static IReverseProxyBuilder AddServiceDiscoveryBackendResolver(this IReverseProxyBuilder builder)
    {
        builder.Services.AddSingleton<IDestinationResolver, BackendAddressResolver>();
        return builder;
    }
}