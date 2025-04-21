namespace AdvanceFileUpload.Client.Helper;

/// <summary>
/// Represents the status of a connection.
/// </summary>
public enum ConnectionStatus
{
    /// <summary>
    /// The connection is healthy.
    /// </summary>
    Healthy,

    /// <summary>
    /// The connection is unhealthy.
    /// </summary>
    Unhealthy,

    /// <summary>
    /// The connection is degraded.
    /// </summary>
    Degraded,

    /// <summary>
    /// The connection has timed out.
    /// </summary>
    Timeout
}
