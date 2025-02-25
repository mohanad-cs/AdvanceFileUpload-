using AdvanceFileUpload.Application.Compression;

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
        public long FileSize { get; init; }

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
        /// Represents the compression options for a file upload session.
        /// </summary>
        public class CompressionOption
        {
            /// <summary>
            /// Gets the compression algorithm to use.
            /// </summary>
            public CompressionAlgorithmOption Algorithm { get; init; }

            /// <summary>
            /// Gets the compression level to use.
            /// </summary>
            public CompressionLevelOption Level { get; init; }
        }
    }
}
