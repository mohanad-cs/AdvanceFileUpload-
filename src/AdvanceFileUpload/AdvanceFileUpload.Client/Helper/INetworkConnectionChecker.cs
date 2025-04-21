namespace AdvanceFileUpload.Client.Helper;

/// <summary>
/// Defines methods to check the health of a network connection.
/// </summary>
public interface INetworkConnectionChecker : IDisposable
{
    /// <summary>
    /// Checks the health of the API asynchronously.
    /// </summary>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the connection status.</returns>
    Task<ConnectionStatus> CheckApiHealthAsync(CancellationToken ct = default);
}
