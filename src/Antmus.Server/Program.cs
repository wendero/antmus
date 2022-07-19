using Antmus.Server;
using Microsoft.AspNetCore.Cors.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<RecorderEngine>();
builder.Services.AddSingleton<MockEngine>();
builder.Services.AddSingleton<MockHelper>();
builder.Services.AddSingleton<CustomMockHelper>();
builder.Services.AddControllers();

var origins = builder.Configuration.GetValue<string>("AllowedOrigins");
var corsEnabled = !string.IsNullOrEmpty(origins);

if (corsEnabled)
{
    var corsBuilder = new CorsPolicyBuilder();
    corsBuilder.AllowAnyMethod();
    corsBuilder.AllowAnyHeader();

    if (origins == "*")
    {
        corsBuilder.AllowAnyOrigin();
    }
    else
    {
        corsBuilder.WithOrigins(origins.Split(';'));
        corsBuilder.AllowCredentials();
    }

    var corsDefaultPolicy = corsBuilder.Build();
    builder.Services.AddCors(o => o.AddDefaultPolicy(corsDefaultPolicy));
}

builder.Services.AddSingleton<IHttpEngine>((provider) =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    return configuration.GetValue<string>("Mode") switch
    {
        "Recorder" => provider.GetRequiredService<RecorderEngine>(),
        _ => provider.GetRequiredService<MockEngine>()
    };
});

var app = builder.Build();

var mode = app.Configuration.GetValue<string>("Mode") ?? "Mock";
var isMockMode = mode == "Mock";
app.Logger.LogInformation("Antmus mode: {mode}", mode);

var redirect = app.Configuration.GetValue<string>("Redirect");

if (!isMockMode)
{
    if (string.IsNullOrEmpty(redirect))
    {
        app.Logger.LogError("Redirect Url is not set. Antmus cannot run in Recorder mode.");
        Environment.Exit(1);
    }
    app.Logger.LogInformation("Redirecting to {redirect}", redirect);
}

if (corsEnabled)
{
    app.Logger.LogInformation("CORS enabled.");
    app.UseCors();
}

app.MapControllers();
app.Map("/{*args}", (HttpContext context) => context.RequestServices.GetService<IHttpEngine>()!.Handle(context));

#region Warm Up
app.Services.GetRequiredService<IHttpEngine>();
app.Services.GetRequiredService<CustomMockHelper>();
#endregion

app.Run();
