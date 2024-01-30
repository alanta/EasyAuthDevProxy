using EasyAuthDevProxy.Infrastructure;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddRazorPages();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddConfigFilter<SetBackendAddressFilter>()
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(EasyAuth.EasyAuthTransform);
    });

var app = builder.Build();

app.UseStaticFiles("/.auth/assets");

app.UseRouting();

app.MapGet("/health", () => "OK");
app.MapGet("/.auth/logout", EasyAuth.Logout);

app.MapRazorPages();

app.MapReverseProxy();

app.Run();
