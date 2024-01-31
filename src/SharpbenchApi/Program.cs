using MirrorSharp.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapMirrorSharp("/mirrorsharp");
app.MapGet("/", () => "Hello World!");

app.Run();
