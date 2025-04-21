namespace AdvanceFileUpload.API
{
    public class APIKey
    {
        public required string ClientId { get; init; }
        public required string Key { get; init; }
        public ClientRateLimit? RateLimit { get; init; } // if null then it means there is no rate limit for that client
    }

    public class ApiKeyOptions
    {
        public const string SectionName = "APIKeyOptions";
        public List<APIKey> APIKeys { get; set; } = new();
        public bool EnableAPIKeyAuthentication { get; set; } = false;
        public bool EnableRateLimiting { get; set; } = false;
        public int DefaultMaxRequestsPerMinute { get; set; }=1000; // Default value for rate limiting
    }

    public class ClientRateLimit
    {
        public int RequestsPerMinute { get; set; }
    }
}
