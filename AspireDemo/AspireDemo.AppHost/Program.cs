using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var demoApp = builder.AddProject<Projects.AspireDemo_App>("demoapp");

// Add EasyAuth proxy, forwarding to the demo app via service discovery. Runs as a plain .NET
// process by default; use AddEasyAuthProxyContainer(...) instead if you need container isolation.
var easyAuthProxy = builder
    .AddEasyAuthProxy("easyauth")
    .WithHostPort(8888)
    .WithBackend(demoApp);

builder.Build().Run();
