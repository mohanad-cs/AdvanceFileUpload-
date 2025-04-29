namespace AdvanceFileUpload.API.Security
{
    /// <summary>
    /// Represents the configuration options for API key authentication and rate limiting.
    /// </summary>
    public class ApiKeyOptions
    {
        /// <summary>
        /// The name of the configuration section for API key options.
        /// </summary>
        public const string SectionName = "APIKeyOptions";

        /// <summary>
        /// A list of API keys used for authentication and rate limiting.
        /// </summary>
        public List<APIKey> APIKeys { get; set; } = new();

        /// <summary>
        /// Indicates whether API key authentication is enabled.
        /// </summary>
        public bool EnableAPIKeyAuthentication { get; set; } = false;

        /// <summary>
        /// Indicates whether rate limiting is enabled.
        /// </summary>
        public bool EnableRateLimiting { get; set; } = false;

        /// <summary>
        /// The default maximum number of requests allowed per minute for rate limiting.
        /// </summary>
        public int DefaultMaxRequestsPerMinute { get; set; } = 1000; // Default value for rate limiting
    }
}
