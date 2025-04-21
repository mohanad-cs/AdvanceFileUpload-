namespace AdvanceFileUpload.API.Security
{
    /// <summary>
    /// Represents an API key used for authentication and rate limiting.
    /// </summary>
    public class APIKey
    {
        /// <summary>
        /// The name of the client associated with the API key.
        /// </summary>
        public required string ClientId { get; init; }
        /// <summary>
        /// The API key string used for authentication.
        /// </summary>
        public required string Key { get; init; }
        /// <summary>
        /// The RateLimit associated with the API key.
        /// </summary>
        /// <remarks>if <see cref="RateLimit"/> set to <see cref="null"/> then it means there is no rate limit for that client</remarks>
        public ClientRateLimit? RateLimit { get; init; }
        // 
    }
}
