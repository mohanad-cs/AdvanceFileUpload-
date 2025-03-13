using AdvanceFileUpload.Application.Request;
using System.Reflection;

namespace AdvanceFileUpload.Client
{
    public class UploadOptions
    {
        public CompressionOption? CompressionOption { get; set; }
        public string TempDirectory { get; set; } = Path.GetTempPath();

        public int MaxConcurrentUploads { get; set; } = 4; // Default to 4 concurrent uploads
        public int MaxRetriesCount { get; set; } = 3; // Default to 3 retries
    }
}
