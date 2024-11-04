using EasyAuthDevProxy.Infrastructure;
using Yarp.ReverseProxy.Transforms;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddServiceDiscovery();

builder.Services.AddRazorPages();

// Need to use the full reverse proxy to be able to add the EasyAuth headers into the forwarded
// request.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(EasyAuth.EasyAuthTransform);
    })
    .AddServiceDiscoveryBackendResolver();

var app = builder.Build();

app.UseStaticFiles("/.auth/assets");

app.UseRouting();

app.MapGet("/health", () => "OK");
app.MapGet("/.auth/logout", EasyAuth.Logout);

app.MapRazorPages();

app.MapReverseProxy();


app.Run();

public partial class Program;