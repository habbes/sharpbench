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

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/jobs-ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            await runner.RealTimeSyncWithClient(webSocket);
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
    else
    {
        await next(context);
    }
});

app.Run();



record RunArgs(string Code);
