using Sharpbench.Runner;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisClient("SharpbenchCache");
builder.Services.AddSharpbench();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
