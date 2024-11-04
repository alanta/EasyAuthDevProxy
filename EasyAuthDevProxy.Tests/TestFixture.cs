using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasyAuthDevProxy.Tests;

public class EasyAuthDevProxyApplicationFactory : WebApplicationFactory<Program>
{
    private readonly List<(string key, string? value)> _settings = new ();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureWebHost(cfg =>
        {
            foreach (var setting in _settings)
            {
                cfg.UseSetting(setting.key, setting.value);
            }
        });
        
        return base.CreateHost(builder);
    }
    
    public EasyAuthDevProxyApplicationFactory WithSetting(string key, string? value)
    {
        _settings.Add((key, value));
        return this;
    }
}