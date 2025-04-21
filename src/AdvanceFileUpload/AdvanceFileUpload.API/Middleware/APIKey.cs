namespace AdvanceFileUpload.API
{
    public class APIKey
    {
        public required string ClientId { get; init; }
        public required string Key { get; init; }
    }

    public class ApiKeyOptions
    {
        public const string SectionName = "APIKeyOptions";
        public List<APIKey>? APIKeys { get; set; }
        public bool EnableAPIKeyAuthentication { get; set; } = false;
    }
}
