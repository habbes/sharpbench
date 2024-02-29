using Sharpbench.Runner;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSharpbench();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
