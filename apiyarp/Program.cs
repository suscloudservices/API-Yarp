using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Rensa och stäng av onödig loggning
builder.Logging.ClearProviders(); // Tar bort alla loggningsleverantörer

// Ladda YARP-konfiguration och konfigurera proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddConfigFilter<EnvConfigFilter>();

// Lägg till autentisering (JWT och OpenID Connect)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = Environment.GetEnvironmentVariable("AUTH_AUTHORITY") 
                            ?? throw new InvalidOperationException("AUTH_AUTHORITY saknas");
        options.Audience = Environment.GetEnvironmentVariable("AUTH_AUDIENCE") 
                            ?? throw new InvalidOperationException("AUTH_AUDIENCE saknas");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

// Bygg applikationen
var app = builder.Build();

// Lägg till landrestriktioner via middleware
app.UseMiddleware<CountryRestrictionMiddleware>();

// Lägg till autentisering och auktorisering
app.UseAuthentication();
app.UseAuthorization();

// Endpoint för att visa instansinformation
app.MapGet("/instance", () =>
{
    string instanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? "Instance ID not found";
    string appName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "App Name not found";
    return Results.Ok(new { source = appName, InstanceId = instanceId });
});

// Endpoint för att visa inkommande headers
app.MapGet("/show-headers", async (HttpContext context) =>
{
    var headers = context.Request.Headers;
    return Results.Json(headers);
});

// Lägg till proxy med krav på autentisering
app.MapReverseProxy().RequireAuthorization();

// Kör applikationen
app.Run();
