using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Configure OpenID Connect authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddConfigFilter<EnvConfigFilter>();

var app = builder.Build();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<CountryRestrictionMiddleware>();

app.MapGet("/instance", () =>
{
    string instanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? "Instance ID not found";
    string appName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "App Name not found";
    return Results.Ok(new { source = appName, InstanceId = instanceId });
}).RequireAuthorization();

app.MapGet("/show-headers", async (HttpContext context) =>
{
    var headers = context.Request.Headers;
    return Results.Json(headers);
}).RequireAuthorization();

// Protect reverse proxy with authentication
app.MapReverseProxy().RequireAuthorization();

app.Run();
