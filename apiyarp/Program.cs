using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Remove logging setup entirely
builder.Logging.ClearProviders(); // Clears logging providers without adding Console/Debug

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddConfigFilter<EnvConfigFilter>();

// Add Authentication (e.g., OpenID Connect/JWT Bearer)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = Environment.GetEnvironmentVariable("AUTH_AUTHORITY") ?? throw new InvalidOperationException("AUTH_AUTHORITY not set");
        options.Audience = Environment.GetEnvironmentVariable("AUTH_AUDIENCE") ?? throw new InvalidOperationException("AUTH_AUDIENCE not set");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };
    });

var app = builder.Build();

// Add middleware for auth and country restriction
app.UseMiddleware<CountryRestrictionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapGet("/instance", () =>
{
    string instanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? "Instance ID not found";
    string appName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "App Name not found";
    return Results.Ok(new { source = appName, InstanceId = instanceId });
});

app.MapGet("/show-headers", async (HttpContext context) =>
{
    var headers = context.Request.Headers;
    return Results.Json(headers);
});

// Protect reverse proxy with authorization
app.MapReverseProxy().RequireAuthorization();

app.Run();
