using AdvanceFileUpload.Application.Request;

namespace AdvanceFileUpload.Client
{
    /// <summary>
    /// Represents the options for uploading files, including compression settings, excluded file extensions, 
    /// temporary directory, maximum concurrent uploads, and maximum retry count.
    /// </summary>
    public class UploadOptions
    {
        /// <summary>
        /// Gets or sets the compression option for the file upload.
        /// </summary>
        public CompressionOption? CompressionOption { get; set; }

        /// <summary>
        /// Gets the list of file extensions that should not be compressed.
        /// </summary>
        public List<string> ExcludedCompressionExtensions { get; init; } = new List<string>();

        /// <summary>
        /// Gets or sets the temporary directory used for file uploads.
        /// </summary>
        public string TempDirectory { get; set; } = Path.GetTempPath();

        /// <summary>
        /// Gets or sets the maximum number of concurrent uploads. Default is 4.
        /// </summary>
        public int MaxConcurrentUploads { get; set; } = 4; // Default to 4 concurrent uploads

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for failed uploads. Default is 3.
        /// </summary>
        public int MaxRetriesCount { get; set; } = 3; // Default to 3 retries
        /// <summary>
        /// Gets the delay between retries in seconds. Default is 5 seconds.
        /// </summary>
        public int DefaultRetryDelayInSeconds { get; } = 5; // Default to 5 second
        /// <summary>
        /// Gets or sets the API key for authentication.
        /// </summary>
        public required string APIKey { get; set; }

    }
}
