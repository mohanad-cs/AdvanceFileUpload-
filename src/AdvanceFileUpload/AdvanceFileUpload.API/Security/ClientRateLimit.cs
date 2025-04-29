namespace AdvanceFileUpload.API.Security
{
    /// <summary>
    /// Represents the rate limit configuration for a client.
    /// </summary>
    public class ClientRateLimit
    {
        /// <summary>
        /// Gets or sets the maximum number of requests a client can make per minute.
        /// </summary>
        public int RequestsPerMinute { get; set; }
    }
}
