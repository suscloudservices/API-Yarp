using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from environment variables
string azureAdInstance = Environment.GetEnvironmentVariable("AZURE_AD_INSTANCE") ?? "https://login.microsoftonline.com/";
string azureAdTenantId = Environment.GetEnvironmentVariable("AZURE_AD_TENANT_ID") ?? throw new ArgumentNullException("AZURE_AD_TENANT_ID");
string azureAdClientId = Environment.GetEnvironmentVariable("AZURE_AD_CLIENT_ID") ?? throw new ArgumentNullException("AZURE_AD_CLIENT_ID");
string azureAdCallbackPath = Environment.GetEnvironmentVariable("AZURE_AD_CALLBACK_PATH") ?? "/signin-oidc";

// Configure Azure AD authentication using environment variables
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        options.Instance = azureAdInstance;
        options.TenantId = azureAdTenantId;
        options.ClientId = azureAdClientId;
        options.CallbackPath = azureAdCallbackPath;
    });

builder.Services.AddAuthorization();

// Add YARP services
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddConfigFilter<EnvConfigFilter>();

var app = builder.Build();

// Add middleware for authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Existing middleware
app.UseMiddleware<CountryRestrictionMiddleware>();

// Map endpoints
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

// Map reverse proxy
app.MapReverseProxy();

app.Run();
