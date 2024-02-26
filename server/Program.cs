using MirrorSharp.AspNetCore;
using Sharpbench;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();

// TODO: use proper dependency injection
var jobs = new JobsTracker();
var runner = new JobRunner(jobs);
runner.RunJobs();

var app = builder.Build();

// See: https://github.com/ashmind/mirrorsharp
app.UseWebSockets();
app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod());


app.MapMirrorSharp("/mirrorsharp");

app.MapPost("/run", (RunArgs args) =>
{
    Console.WriteLine($"Received code ${args.Code}");
    var result = jobs.SubmitJob(args.Code);
    return result;
});

app.Run();



record RunArgs(string Code);
