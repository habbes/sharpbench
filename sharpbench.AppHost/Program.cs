var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("SharpbenchCache");

var sharpbenchapi = builder.AddProject<Projects.SharpbenchApi>("SharpbenchApi")
    .WithReference(cache);

var sharpbenchrunner = builder.AddProject<Projects.SharpbenchRunner>("SharpbenchRunner")
    .WithReference(cache);

builder.AddNpmApp("SharpbenchUi", "../webapp", "dev")
    .WithReference(sharpbenchapi)
    .WithReference(sharpbenchrunner)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();
builder.Build().Run();
