using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// Configure Azure AD authentication
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// Add Authorization
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy; // All requests require authentication
});

// Add YARP services with reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddConfigFilter<EnvConfigFilter>();

var app = builder.Build();

// Add Middleware for authentication and authorization
app.UseAuthentication(); // Authentication middleware
app.UseAuthorization();  // Authorization middleware

// Keep existing middleware
app.UseMiddleware<CountryRestrictionMiddleware>();

// Map endpoints
app.MapGet("/instance", () =>
{
    string instanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? "Instance ID not found";
    string appName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "App Name not found";
    return Results.Ok(new { source = appName, InstanceId = instanceId });
}).RequireAuthorization(); // Protect this endpoint

app.MapGet("/show-headers", async (HttpContext context) =>
{
    var headers = context.Request.Headers;
    return Results.Json(headers);
}).RequireAuthorization(); // Protect this endpoint

// Map reverse proxy
app.MapReverseProxy();

app.Run();
