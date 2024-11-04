using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public class CountryRestrictionMiddleware
{
    private readonly RequestDelegate _next;

    public CountryRestrictionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Retrieve the allowed country from the environment variable
        string allowedCountry = Environment.GetEnvironmentVariable("ALLOWED_COUNTRY");

        // If the ALLOWED_COUNTRY environment variable is not set or is "false", allow access from anywhere
        if (string.IsNullOrEmpty(allowedCountry) || allowedCountry.Equals("false", System.StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Check if the X-Azure-Geo-Country header is present in the request
        if (!context.Request.Headers.TryGetValue("X-Azure-Geo-Country", out var countryHeader))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Access denied: No country information provided.");
            return;
        }

        // Compare the header's value with the allowed country
        if (!countryHeader.ToString().Equals(allowedCountry, System.StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync($"Access denied: Traffic is only allowed from {allowedCountry}.");
            return;
        }

        // Allow the request to continue if the country matches the allowed value
        await _next(context);
    }
}
