using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

public class CountryRestrictionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CountryRestrictionMiddleware> _logger;

    public CountryRestrictionMiddleware(RequestDelegate next, ILogger<CountryRestrictionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string allowedCountry = Environment.GetEnvironmentVariable("ALLOWED_COUNTRY");

        if (string.IsNullOrEmpty(allowedCountry) || allowedCountry.Equals("false", System.StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Country restriction is disabled. Allowing access.");
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Azure-Geo-Country", out var countryHeader))
        {
            _logger.LogWarning("Access denied: No country information provided.");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Access denied: No country information provided.");
            return;
        }

        if (!countryHeader.ToString().Equals(allowedCountry, System.StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Access denied: Traffic from {Country} is not allowed. Only {AllowedCountry} is permitted.", countryHeader.ToString(), allowedCountry);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync($"Access denied: Traffic is only allowed from {allowedCountry}.");
            return;
        }

        _logger.LogInformation("Access granted: Traffic from {Country} is allowed.", countryHeader.ToString());
        await _next(context);
    }
}
