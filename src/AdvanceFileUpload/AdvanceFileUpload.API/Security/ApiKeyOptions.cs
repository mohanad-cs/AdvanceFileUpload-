namespace AdvanceFileUpload.API.Security
{
    public class ApiKeyOptions
    {
        public const string SectionName = "APIKeyOptions";
        public List<APIKey> APIKeys { get; set; } = new();
        public bool EnableAPIKeyAuthentication { get; set; } = false;
        public bool EnableRateLimiting { get; set; } = false;
        public int DefaultMaxRequestsPerMinute { get; set; }=1000; // Default value for rate limiting
    }
}
