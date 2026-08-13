using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var demoApp = builder.AddProject<Projects.AspireDemo_App>("demoapp");

// Add EasyAuth proxy as a container resource, forwarding to the demo app via service discovery
var easyAuthProxy = builder
    .AddEasyAuthProxy("easyauth")
    .WithHostPort(8888)
    .WithBackend(demoApp);

builder.Build().Run();
