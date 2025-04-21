using Microsoft.Extensions.Options;

namespace AdvanceFileUpload.API.Middleware
{
    public sealed class APIKeyMiddleware
    {
        private const string APIKEY = "X-APIKEY";
        private readonly RequestDelegate _next;
        public APIKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task InvokeAsync(HttpContext context, IOptions<ApiKeyOptions> apiKeyOptions)
        {
            if (!apiKeyOptions.Value.EnableAPIKeyAuthentication)
            {
                await _next(context);
                return;
            }
            // check if there is no Api key configured
            if (apiKeyOptions.Value.APIKeys.Count == 0)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("API Key authentication is enabled but no API keys are configured.");
                return;
            }
            // Check if the request path is not for the health check
            if (context.Request.Path.StartsWithSegments("/health"))
            {
                await _next(context);
                return;
            }
            // Check if the API key is present in the request headers
            if (!context.Request.Headers.TryGetValue(APIKEY, out var extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key was not provided.");
                return;
            }
            // Validate the API key
            if (!apiKeyOptions.Value.APIKeys.Any(x=>x.Key.Equals(extractedApiKey)))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized client.");
                return;
            }
            await _next(context);
        }
    }
}
