using Microsoft.AspNetCore.Mvc;
using MirrorSharp.AspNetCore;
using Sharpbench.Core;
using SharpbenchApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
builder.Services.AddSharpbench();
builder.Services.AddSingleton<RealtimeClientsNotifier>();
builder.Services.AddHostedService<RealtimeMessagesWorker>();

var app = builder.Build();

// See: https://github.com/ashmind/mirrorsharp
app.UseWebSockets();
app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod());


app.MapMirrorSharp("/mirrorsharp");

app.MapPost("/run", async ([FromServices] IJobRepository jobs, RunArgs args) =>
{
    var job = await jobs.SubmitJob(args.Code, args.ClientId);
    return job;
});

app.MapGet("/jobs/{jobId}", async ([FromServices] IJobRepository jobs, [FromRoute] string jobId) =>
{
    var job = await jobs.GetJob(jobId);
    return job;
});

app.MapGet("/status", () =>
{
    return new { Ok = true };
});

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/jobs-ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var clientsNotifier = context.RequestServices.GetRequiredService<RealtimeClientsNotifier>();
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var clientId = context.Request.Query["sessionId"].FirstOrDefault();
            if (!string.IsNullOrEmpty(clientId))
            {
                await clientsNotifier.RealTimeSyncWithClient(webSocket, clientId);
            }
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

record RunArgs(string Code, string ClientId);
