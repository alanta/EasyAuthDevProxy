var builder = DistributedApplication.CreateBuilder(args);

var demoApp = builder.AddProject<Projects.AspireDemo_App>("demoapp");

var easyAuth = builder.AddProject<Projects.EasyAuthDevProxy>("easyauthproxy")
    .WithEnvironment("BACKEND", "https://demoapp")
    .WithReference(demoApp)
    .WithExternalHttpEndpoints();

/*
builder.AddContainer("easyauthproxy", "ghcr.io/alanta/easyauthdevproxy", "1.0.1-11")
    .WithContainerRuntimeArgs("--add-host=host.docker.internal:host-gateway", "-p", "8080:8888","-e","backend=https://demoapp")
    .WithReference(demoApp)
    .WithHttpEndpoint(port: 8888, targetPort: 8080, isProxied: false);*/
    
builder.Build().Run();
