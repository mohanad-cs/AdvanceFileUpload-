using AdvanceFileUpload.API.Security;
using AdvanceFileUpload.Application.Shared;
using Microsoft.Extensions.Options;

namespace AdvanceFileUpload.API.Middleware
{
    /// <summary>
    /// Middleware to handle API Key authentication for incoming HTTP requests.
    /// </summary>
    public sealed class APIKeyMiddleware
    {
        private const string APIKEY = "X-APIKEY";
        private readonly RequestDelegate _next;
        private readonly ILogger<APIKeyMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="APIKeyMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger instance for logging information.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="next"/> or <paramref name="logger"/> is null.</exception>
        public APIKeyMiddleware(RequestDelegate next, ILogger<APIKeyMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Middleware invocation method to process the HTTP request.
        /// </summary>
        /// <param name="context">The HTTP context of the current request.</param>
        /// <param name="apiKeyOptions">The API key options configuration.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> or <paramref name="apiKeyOptions"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when API key authentication is not properly configured.</exception>
        public async Task InvokeAsync(HttpContext context, IOptionsMonitor<ApiKeyOptions> apiKeyOptions)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (apiKeyOptions is null)
            {
                throw new ArgumentNullException(nameof(apiKeyOptions));
            }

            if (apiKeyOptions.CurrentValue is null)
            {
                throw new InvalidOperationException("API Key authentication is not configured!");
            }

            if (!apiKeyOptions.CurrentValue.EnableAPIKeyAuthentication)
            {
                _logger.LogWarning("API Key authentication is disabled.");
                await _next(context);
                return;
            }

            if (apiKeyOptions.CurrentValue.APIKeys.Count == 0)
            {
                _logger.LogError("API Key authentication is enabled but no API keys are configured.");
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("API Key authentication is enabled but no API keys are configured.");
                return;
            }
            if (context.Request.Path.StartsWithSegments("/"+RouteTemplates.Base+"health"))
            {
                await _next(context);
                return;
            }
            if (context.Request.Path.StartsWithSegments(RouteTemplates.APIHealthEndPoint))
            {
                _logger.LogInformation("Health check endpoint accessed.");
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue(APIKEY, out var extractedApiKey))
            {
                _logger.LogWarning("API Key was not provided in the request headers.");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key was not provided.");
                return;
            }

            if (!apiKeyOptions.CurrentValue.APIKeys.Any(x => x.Key.Equals(extractedApiKey)))
            {
                _logger.LogWarning("Unauthorized client attempted to access the API with an invalid API Key.");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized client.");
                return;
            }

            _logger.LogInformation("Valid API Key provided. Passing request to the next middleware.");
            await _next(context);
        }
    }
}
