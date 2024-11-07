using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddConfigFilter<EnvConfigFilter>();

var app = builder.Build();

app.UseMiddleware<CountryRestrictionMiddleware>();

app.MapGet("/instance", (ILogger<Program> logger) =>
{
    string instanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? "Instance ID not found";
    string appName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "App Name not found";
    logger.LogInformation("Accessed /instance endpoint. AppName: {AppName}, InstanceId: {InstanceId}", appName, instanceId);
    return Results.Ok(new { source = appName, InstanceId = instanceId });
});

app.MapGet("/show-headers", async (HttpContext context, ILogger<Program> logger) =>
{
    var headers = context.Request.Headers;
    var headerList = new List<string>();

    foreach (var header in headers)
    {
        string headerInfo = $"{header.Key}: {header.Value}";
        headerList.Add(headerInfo);
        logger.LogDebug("Header - {HeaderKey}: {HeaderValue}", header.Key, header.Value.ToString());
    }

    return Results.Json(headers);
});

app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Proxying request: {Method} {Path}", context.Request.Method, context.Request.Path);
        await next.Invoke();
        logger.LogInformation("Response status code: {StatusCode}", context.Response.StatusCode);
    });
});

app.Run();
