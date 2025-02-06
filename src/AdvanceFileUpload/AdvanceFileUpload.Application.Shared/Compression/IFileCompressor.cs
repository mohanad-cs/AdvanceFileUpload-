
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
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CompressFileAsync(string inputFilePath, string outputDirectory, CancellationToken cancellationToken = default);

        /// <summary>
        /// Compresses multiple files asynchronously.
        /// </summary>
        /// <param name="inputFilePaths">An array of file paths to compress.</param>
        /// <param name="outputDirectory">The directory where the compressed files will be saved.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CompressFilesAsync(string[] inputFilePaths, string outputDirectory, CancellationToken cancellationToken = default);

        /// <summary>
        /// Decompresses a single file asynchronously.
        /// </summary>
        /// <param name="inputFilePath">The path of the file to decompress.</param>
        /// <param name="outputDirectory">The directory where the decompressed file will be saved.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DecompressFileAsync(string inputFilePath, string outputDirectory, CancellationToken cancellationToken = default);

        /// <summary>
        /// Decompresses multiple files asynchronously.
        /// </summary>
        /// <param name="inputFilePaths">An array of file paths to decompress.</param>
        /// <param name="outputDirectory">The directory where the decompressed files will be saved.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DecompressFilesAsync(string[] inputFilePaths, string outputDirectory, CancellationToken cancellationToken = default);
    }
}