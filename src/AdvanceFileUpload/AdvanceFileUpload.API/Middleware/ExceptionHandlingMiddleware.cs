using AdvanceFileUpload.Domain.Core;
using Azure.Core;
using System.Net;

namespace AdvanceFileUpload.API.Middleware
{
    /// <summary>
    /// Middleware to handle exceptions globally.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger instance.</param>
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Invokes the middleware to handle the HTTP context.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task that represents the completion of request processing.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// Handles the exception and writes an appropriate response.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <returns>A task that represents the completion of exception handling.</returns>
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "An unexpected error occurred.");

            // More log stuff        

            ExceptionResponse response = exception switch
            {
                OperationCanceledException _ => new ExceptionResponse(HttpStatusCode.NoContent, "Operation was canceled."),
                ApplicationException _ => new ExceptionResponse(HttpStatusCode.BadRequest, $"Application exception occurred.{exception.Message}"),
                DomainException _ => new ExceptionResponse(HttpStatusCode.BadRequest, $"Domain exception occurred.{exception.Message}"),
                _ => new ExceptionResponse(HttpStatusCode.InternalServerError, "Internal server error. Please retry later.")
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)response.StatusCode;
            await context.Response.WriteAsJsonAsync(response);
        }
    }
    /// <summary>
    /// Represents the response for an exception.
    /// </summary>
    /// <param name="StatusCode">The HTTP status code.</param>
    /// <param name="Description">The description of the exception.</param>
    public record ExceptionResponse(HttpStatusCode StatusCode, string Description);

}
