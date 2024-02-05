using MirrorSharp.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// See: https://github.com/ashmind/mirrorsharp
app.UseWebSockets();
app.MapMirrorSharp("/mirrorsharp");


app.Run();

