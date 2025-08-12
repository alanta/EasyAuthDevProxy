using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var demoApp = builder.AddProject<Projects.AspireDemo_App>("demoapp");

// Add EasyAuth proxy as an executable resource from the NuGet package
var easyAuthProxy = builder.AddEasyAuthProxyExecutable("easyauth")
    .WithBackend(demoApp);
    
builder.Build().Run();
