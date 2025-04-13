using AdvanceFileUpload.Application.Compression;

namespace AdvanceFileUpload.Application.Request
{


    /// <summary>
    /// Represents the compression options for a file upload session.
    /// </summary>
    public sealed record CompressionOption
    {


        /// <summary>
        /// Gets the compression algorithm to use.
        /// </summary>
        public CompressionAlgorithmOption Algorithm { get; init; }

        /// <summary>
        /// Gets the compression level to use.
        /// </summary>
        public CompressionLevelOption Level { get; init; }

        /// <summary>
        /// sets the excluded file extensions that should not be compressed.
        /// </summary>
        public List<string> ExcludedCompressionExtensions { get; init; } = new List<string>();

    }
}
