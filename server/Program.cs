using MirrorSharp.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();

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
    return  new RunResult(Id: "1");
});

app.Run();

record RunArgs(string Code);
record RunResult(string Id);
