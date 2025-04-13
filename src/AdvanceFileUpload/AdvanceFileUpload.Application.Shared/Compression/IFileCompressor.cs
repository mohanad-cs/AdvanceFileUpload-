
namespace AdvanceFileUpload.Application.Compression
{
    /// <summary>
    /// Interface for file compression and decompression operations.
    /// </summary>
    public interface IFileCompressor
    {
        /// <summary>
        /// Compresses a single file asynchronously.
        /// </summary>
        /// <param name="inputFilePath">The path of the file to compress.</param>
        /// <param name="outputDirectory">The directory where the compressed file will be saved.</param>
        /// <param name="compressionAlgorithmOption">The compression algorithm to use.</param>
        /// <param name="compressionLevelOption">The level of compression to apply.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CompressFileAsync(string inputFilePath, string outputDirectory, CompressionAlgorithmOption compressionAlgorithmOption, CompressionLevelOption compressionLevelOption, CancellationToken cancellationToken = default);

        /// <summary>
        /// Compresses multiple files asynchronously.
        /// </summary>
        /// <param name="inputFilePaths">An array of file paths to compress.</param>
        /// <param name="outputDirectory">The directory where the compressed files will be saved.</param>
        /// <param name="compressionAlgorithmOption">The compression algorithm to use.</param>
        /// <param name="compressionLevelOption">The level of compression to apply.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CompressFilesAsync(string[] inputFilePaths, string outputDirectory, CompressionAlgorithmOption compressionAlgorithmOption, CompressionLevelOption compressionLevelOption, CancellationToken cancellationToken = default);

        /// <summary>
        /// Decompresses a single file asynchronously.
        /// </summary>
        /// <param name="inputFilePath">The path of the file to decompress.</param>
        /// <param name="outputDirectory">The directory where the decompressed file will be saved.</param>
        /// <param name="compressionAlgorithmOption">The compression algorithm to use.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DecompressFileAsync(string inputFilePath, string outputDirectory, CompressionAlgorithmOption compressionAlgorithmOption, CancellationToken cancellationToken = default);

        /// <summary>
        /// Decompresses multiple files asynchronously.
        /// </summary>
        /// <param name="inputFilePaths">An array of file paths to decompress.</param>
        /// <param name="outputDirectory">The directory where the decompressed files will be saved.</param>
        /// <param name="compressionAlgorithmOption">The compression algorithm to use.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DecompressFilesAsync(string[] inputFilePaths, string outputDirectory, CompressionAlgorithmOption compressionAlgorithmOption, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines whether a file is applicable for compression.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        bool IsFileApplicableForCompression(string filePath);
        /// <summary>
        /// Adds an excluded file extension to the list of extensions that should not be compressed.
        /// </summary>
        /// <param name="extension"></param>
        void AddExcludedExtension(string extension);
    }
}