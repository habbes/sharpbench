var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("sharpbench-cache");

var sharpbenchapi = builder.AddProject<Projects.SharpbenchApi>("sharpbench-api")
    .WithReference(cache);

var sharpbenchrunner = builder.AddProject<Projects.SharpbenchRunner>("sharpbench-runner")
    .WithReference(cache);

builder.AddNpmApp("sharpbench-ui", "../webapp", "dev")
    .WithReference(sharpbenchapi)
    .WithReference(sharpbenchrunner)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();
builder.Build().Run();
