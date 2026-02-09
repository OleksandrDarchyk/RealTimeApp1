using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using api;
using api.Etc;
using dataccess;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.Generation.Processors.Security;
using StackExchange.Redis;
using StateleSSE.AspNetCore;
using StateleSSE.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// =========================
// AppOptions
// =========================
builder.Services.AddSingleton<AppOptions>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var options = new AppOptions();
    configuration.GetSection(nameof(AppOptions)).Bind(options);
    return options;
});

// =========================
// Fast shutdown (important for SSE)
// =========================
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(0);
});

// =========================
// Database (Postgres via EF Core)
// =========================
builder.Services.AddDbContext<ChatDbContext>((sp, opt) =>
{
    var appOptions = sp.GetRequiredService<AppOptions>();

    if (string.IsNullOrWhiteSpace(appOptions.DbConnectionString))
        throw new InvalidOperationException("AppOptions:DbConnectionString is missing");

    opt.UseNpgsql(appOptions.DbConnectionString);
});

// =========================
// Redis (StackExchange.Redis) + StateleSSE backplane
// =========================
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var appOptions = sp.GetRequiredService<AppOptions>();

    if (string.IsNullOrWhiteSpace(appOptions.RedisConnectionString))
        throw new InvalidOperationException("AppOptions:RedisConnectionString is missing");

    var config = ConfigurationOptions.Parse(appOptions.RedisConnectionString);
    config.AbortOnConnectFail = false;

    return ConnectionMultiplexer.Connect(config);
});

builder.Services.AddRedisSseBackplane();

// =========================
// Authentication & Authorization (JWT)
// =========================
builder.Services.AddSingleton<JwtService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        var secret = builder.Configuration["AppOptions:Secret"]
                     ?? throw new InvalidOperationException("JWT Secret missing (AppOptions:Secret)");

        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        };
    });

builder.Services.AddAuthorization();

// =========================
// ASP.NET basics
// =========================
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// =========================
// ExceptionHandler
// =========================
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();


builder.Services.AddCors();
builder.Services.AddOpenApiDocument(cfg =>
{
    cfg.Title = "Realtime Chat API";

    // Adds a JWT Bearer auth scheme to Swagger
    cfg.AddSecurity("Bearer", Enumerable.Empty<string>(), new OpenApiSecurityScheme
    {
        Type = OpenApiSecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = OpenApiSecurityApiKeyLocation.Header,
        Name = "Authorization",
        Description = "Paste your JWT token here. Example: Bearer {your_token}"
    });

    // Apply Bearer auth to endpoints that use [Authorize]
    cfg.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
});

// =========================
// Build app
// =========================
var app = builder.Build();

// =========================
// Validate AppOptions early (clear errors on Fly/local)
// =========================
var opts = app.Services.GetRequiredService<AppOptions>();
Validator.ValidateObject(opts, new ValidationContext(opts), validateAllProperties: true);

// =========================
// Exception
// =========================
app.UseExceptionHandler();


// =========================
// Middleware pipeline
// =========================
app.UseRouting();

app.UseCors(c =>
    c.AllowAnyHeader()
     .AllowAnyMethod()
     .AllowAnyOrigin()
     .SetIsOriginAllowed(_ => true));

app.UseAuthentication();
app.UseAuthorization();

app.UseOpenApi();
app.UseSwaggerUi();

app.MapControllers();

// =========================
// Generate TS client in Development
// =========================
if (app.Environment.IsDevelopment())
{
    app.GenerateApiClientsFromOpenApi(
        "../../client/src/generated-ts-client.ts",
        "../../client/openapi.json"
    ).GetAwaiter().GetResult();
}

// =========================
// SSE disconnect handling
// =========================
var backplane = app.Services.GetRequiredService<ISseBackplane>();
backplane.OnClientDisconnected += async (_, e) =>
{
    foreach (var group in e.Groups)
    {
        await backplane.Clients.SendToGroupAsync(group, new
        {
            eventType = "SystemMessage",
            message = "someone disconnected",
            kind = "disconnect"
        });
    }
};

app.Run();
