using System.Net;
using Polly;
using Polly.Timeout;
namespace AdvanceFileUpload.Client;

/// <summary>
/// Checks the health of a network connection by sending HTTP requests to a specified endpoint.
/// </summary>
public sealed class NetworkConnectionChecker : INetworkConnectionChecker
{
    private readonly HttpClient _httpClient;
    private readonly AsyncTimeoutPolicy _timeoutPolicy;
    private readonly NetworkCheckOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkConnectionChecker"/> class.
    /// </summary>
    /// <param name="networkCheckOptions">The options for configuring the network check.</param>
    public NetworkConnectionChecker(NetworkCheckOptions networkCheckOptions)
    {
        _options = networkCheckOptions;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = networkCheckOptions.BaseAddress;
        _httpClient.Timeout = networkCheckOptions.Timeout;
        _timeoutPolicy = Policy.TimeoutAsync(_options.Timeout, TimeoutStrategy.Optimistic);
    }

    ///<inheritdoc/>
    public async Task<ConnectionStatus> CheckApiHealthAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _timeoutPolicy.ExecuteAsync(async token =>
                await _httpClient.SendAsync(CreateHealthRequest(), token), ct);

            return EvaluateResponse(response);
        }
        catch (TimeoutException)
        {
            return ConnectionStatus.Timeout;
        }
        catch (Exception)
        {
            return ConnectionStatus.Unhealthy;
        }
    }

    /// <summary>
    /// Creates the HTTP request message for checking the health endpoint.
    /// </summary>
    /// <returns>The HTTP request message.</returns>
    private HttpRequestMessage CreateHealthRequest() => new(_options.Method, _options.HealthEndpoint)
    {
        Version = _options.UseHttp2 ? HttpVersion.Version20 : HttpVersion.Version11,
        VersionPolicy = HttpVersionPolicy.RequestVersionExact
    };

    /// <summary>
    /// Evaluates the HTTP response and determines the connection status.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <returns>The connection status based on the response.</returns>
    private static ConnectionStatus EvaluateResponse(HttpResponseMessage response)
    {
        return response.StatusCode switch
        {
            HttpStatusCode.OK => ConnectionStatus.Healthy,
            HttpStatusCode.Created => ConnectionStatus.Healthy,
            HttpStatusCode.TooManyRequests => ConnectionStatus.Degraded,
            _ => ConnectionStatus.Unhealthy
        };
    }

    /// <summary>
    /// Disposes the HTTP client.
    /// </summary>
    public void Dispose() => _httpClient?.Dispose();
}
