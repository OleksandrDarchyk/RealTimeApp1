using api;
using StackExchange.Redis;
using StateleSSE.AspNetCore;
using StateleSSE.AspNetCore.Extensions;



var builder = WebApplication.CreateBuilder(args);
// Render multiplexer start again after test local
// builder.Services.AddSingleton<AppOptions>(provider =>
// {
//     var configuration = provider.GetRequiredService<IConfiguration>();
//     var appOptions = new AppOptions();
//     configuration.GetSection(nameof(AppOptions)).Bind(appOptions);
//     return appOptions;
// });

builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(0);
});
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var appOptions = sp.GetRequiredService<AppOptions>();
            
    var config = ConfigurationOptions.Parse(appOptions.RenderConnectionString);
    
    config.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(config);
});
//start again after test local
// builder.Services.AddRedisSseBackplane();



// Local Radis backplane
builder.Services.AddRedisSseBackplane(conf =>
{
    conf.RedisConnectionString = "localhost:6379,abortConnect=false";
});

builder.Services.AddControllers();
builder.Services.AddCors();
builder.Services.AddOpenApiDocument();

var app = builder.Build();

app.UseRouting();

app.UseCors(conf =>
    conf.AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin()
        .SetIsOriginAllowed(_ => true));

app.UseOpenApi();
app.UseSwaggerUi();

app.MapControllers();

var bp = app.Services.GetRequiredService<ISseBackplane>();

bp.OnClientDisconnected += async (_, e) =>
{
    foreach (var group in e.Groups)
    {
        await bp.Clients.SendToGroupAsync(group, new
        {
            eventType = "SystemMessage",
            message = "someone disconnected",
            kind = "disconnect"
        });
    }
};


app.Run();