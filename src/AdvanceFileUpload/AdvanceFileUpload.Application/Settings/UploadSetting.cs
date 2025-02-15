namespace AdvanceFileUpload.Application.Settings
{
    /// <summary>
    ///  Represents the upload settings.
    /// </summary>
    public sealed class UploadSetting : IUploadSetting
    {

        /// <summary>
        /// The name of the section in the configuration file.
        /// </summary>
        public const string SectionName = "UploadSetting";
        /// <summary>
        /// The default maximum chunk file size.
        /// </summary>
        public const long DefaultMaxChunkSize = 1024 * 1024 * 2;
        /// <inheritdoc />
        public required string SavingDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

        /// <inheritdoc />
        public long MaxFileSize { get; set; }

        /// <inheritdoc />
        public required string[] AllowedExtensions { get; set; }

        /// <inheritdoc />
        public long MaxChunkSize { get; set; } = DefaultMaxChunkSize;
        /// <inheritdoc />
        public required string TempDirectory { get; set; }= Path.GetTempPath();
    }
}
