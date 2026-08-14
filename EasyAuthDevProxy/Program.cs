using EasyAuthDevProxy.Infrastructure;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

builder.Services.AddServiceDiscovery();

builder.Services.AddRazorPages();

var useUnsafeHttpsForDevelopment = builder.Environment.IsDevelopment();

// Need to use the full reverse proxy to be able to add the EasyAuth headers into the forwarded
// request.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .ConfigureHttpClient((context, handler) =>
    {
        if (useUnsafeHttpsForDevelopment)
        {
            handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, errors) =>
                errors == System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch;
        }
    })
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
