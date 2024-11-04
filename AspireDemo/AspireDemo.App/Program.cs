using DemoApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

builder.Services.AddRazorPages();

// Enable EasyAuth authentication in Azure Container Apps
builder.Services
    .AddAuthentication(EasyAuth.AUTHSCHEMENAME)
    .AddAzureContainerAppsEasyAuth();

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.MapDefaultEndpoints();

app.Run();
