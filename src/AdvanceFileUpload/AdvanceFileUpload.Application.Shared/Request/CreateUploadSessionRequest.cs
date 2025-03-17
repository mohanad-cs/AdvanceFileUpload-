namespace AdvanceFileUpload.Application.Request
{



    /// <summary>
    /// Represents a request to create a new file upload session.
    /// </summary>
    public sealed record CreateUploadSessionRequest
    {

        /// <summary>
        /// Gets the name of the file to be uploaded.
        /// </summary>
        public required string FileName { get; init; }

        /// <summary>
        /// Gets the size of the file to be uploaded.
        /// </summary>
        public required long FileSize { get; init; }

        /// <summary>
        /// Gets the file extension of the file to be uploaded.
        /// </summary>
        public required string FileExtension { get; init; }

        /// <summary>
        /// Gets the Compression Option of the file upload session.
        /// </summary>
        public CompressionOption? Compression { get; init; }

        /// <summary>
        /// Gets a value indicating whether compression is used.
        /// </summary>
        public bool UseCompression => Compression != null;
        /// <summary>
        /// Gets the unique identifier of the hub connection.
        /// </summary>
        public string? HubConnectionId { get; init; }
    }
}
