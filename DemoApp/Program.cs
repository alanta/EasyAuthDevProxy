using DemoApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// Enable EasyAuth authentication in Azure Container Apps
builder.Services
    .AddAuthentication(EasyAuth.AUTHSCHEMENAME)
    .AddAzureContainerAppsEasyAuth();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
