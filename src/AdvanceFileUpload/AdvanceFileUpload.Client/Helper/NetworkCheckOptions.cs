namespace AdvanceFileUpload.Client.Helper;

/// <summary>
/// Configuration options for the <see cref="NetworkConnectionChecker"/>.
/// </summary>
public class NetworkCheckOptions
{
    /// <summary>
    /// Gets or sets the base address of the API.
    /// </summary>
    public required Uri BaseAddress { get; set; }

    /// <summary>
    /// Gets or sets the health endpoint to check.
    /// </summary>
    public string HealthEndpoint { get; set; } = "/health";

    /// <summary>
    /// Gets or sets the HTTP method to use for the health check.
    /// </summary>
    public HttpMethod Method { get; set; } = HttpMethod.Get;

    /// <summary>
    /// Gets or sets the timeout duration for the health check request.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Gets or sets a value indicating whether to use HTTP/2 for the health check request.
    /// </summary>
    public bool UseHttp2 { get; set; } = true;
}
