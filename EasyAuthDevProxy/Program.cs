using System.Net.Security;
using EasyAuthDevProxy.Infrastructure;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

builder.Services.AddServiceDiscovery();

builder.Services.AddRazorPages();

// Backends in local development serve the ASP.NET Core developer certificate, which fails
// validation in two different ways: the name doesn't match when the backend is reached through
// service discovery, and the certificate is often not trusted at all (on Linux
// `dotnet dev-certs https --trust` only partially succeeds). Tolerating both is what makes the
// proxy work out of the box, but it does mean the backend isn't authenticated at all, so it's an
// explicit setting rather than something implied by the environment name. Defaults on in
// Development, off everywhere else.
var allowUntrustedBackendCertificate = builder.Configuration.GetValue(
    "EasyAuth:AllowUntrustedBackendCertificate",
    builder.Environment.IsDevelopment());

// Need to use the full reverse proxy to be able to add the EasyAuth headers into the forwarded
// request.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .ConfigureHttpClient((context, handler) =>
    {
        if (allowUntrustedBackendCertificate)
        {
            // Accept a name mismatch and an untrusted chain, but still require the backend to
            // actually present a certificate.
            const SslPolicyErrors toleratedCertificateErrors =
                SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors;

            handler.SslOptions.RemoteCertificateValidationCallback = (_, certificate, _, errors) =>
                certificate is not null && (errors & ~toleratedCertificateErrors) == 0;
        }
    })
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(EasyAuth.EasyAuthTransform);
    })
    .AddServiceDiscoveryBackendResolver();

var app = builder.Build();

if (allowUntrustedBackendCertificate)
{
    app.Logger.LogWarning(
        "Backend TLS certificates are not validated: any certificate the backend presents is accepted, " +
        "including untrusted and self-signed ones. Set EasyAuth:AllowUntrustedBackendCertificate to false " +
        "to require a valid certificate.");
}

app.UseStaticFiles("/.auth/assets");

app.UseRouting();

app.MapGet("/health", () => "OK");
app.MapGet("/.auth/logout", EasyAuth.Logout);

app.MapRazorPages();

app.MapReverseProxy();


app.Run();

public partial class Program;
