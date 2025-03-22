namespace AdvanceFileUpload.Application.Settings
{
    /// <summary>
    /// Interface for upload settings.
    /// </summary>
    public interface IUploadSetting
    {
        /// <summary>
        /// Gets or sets the allowed file extensions.
        /// </summary>
        /// <example>
        /// For example: new string[] { ".jpg", ".png", ".pdf" }
        /// </example>
        string[] AllowedExtensions { get; set; }

        /// <summary>
        /// Gets or sets the maximum chunk size for file uploads.
        /// </summary>
        long MaxChunkSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum file size for uploads.
        /// </summary>
        long MaxFileSize { get; set; }

        /// <summary>
        /// Gets or sets the directory where files will be saved.
        /// </summary>
        string SavingDirectory { get; set; }
        /// <summary>
        /// Gets or sets the directory where temporary files will be saved.
        /// </summary>
        string TempDirectory { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to enable compression.
        /// </summary>
        bool EnableCompression { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to enable integration event publishing.
        /// </summary>
        bool EnableIntegrationEventPublishing { get; set; }
    }
}