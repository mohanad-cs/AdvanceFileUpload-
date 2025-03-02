using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace AdvanceFileUpload.Application.Compression
{
    /// <summary>
    /// Provides methods for compressing and decompressing files using various algorithms.
    /// </summary>
    public sealed class FileCompressor : IFileCompressor
    {
        private readonly ILogger<FileCompressor> _logger;
        ///<summary>
        /// Initializes a new instance of the <see cref="FileCompressor"/> class.
        /// </summary>
        /// <param name="logger">The logger instance to use for logging.</param>
        /// <exception cref="ArgumentNullException">Thrown when the logger is null.</exception>
        public FileCompressor(ILogger<FileCompressor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        /// <inheritdoc/>
        public async Task CompressFileAsync(string inputFilePath, string outputDirectory, CompressionAlgorithmOption compressionAlgorithmOption, CompressionLevelOption compressionLevelOption, CancellationToken cancellationToken = default)
        {
            string outputFilePath = Path.Combine(outputDirectory, Path.GetFileName(inputFilePath) + ".gz");
            await CompressFile(inputFilePath, outputFilePath,compressionAlgorithmOption ,compressionLevelOption , cancellationToken);
        }
        /// <inheritdoc/>
        public async Task DecompressFileAsync(string inputFilePath, string outputDirectory,CompressionAlgorithmOption compressionAlgorithmOption , CancellationToken cancellationToken = default)
        {
            string outputFilePath = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(inputFilePath));
            await DecompressFile(inputFilePath, outputFilePath, compressionAlgorithmOption, cancellationToken);
        }
        /// <inheritdoc/>
        public async Task CompressFilesAsync(string[] inputFilePaths, string outputDirectory, CompressionAlgorithmOption compressionAlgorithmOption, CompressionLevelOption compressionLevelOption, CancellationToken cancellationToken = default)
        {
            if (inputFilePaths == null || inputFilePaths.Length == 0)
            {
                throw new ArgumentException("Input file paths cannot be null or empty.", nameof(inputFilePaths));
            }
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));
            }
            var tasks = new Task[inputFilePaths.Length];
            for (int i = 0; i < inputFilePaths.Length; i++)
            {
                int index = i;
                tasks[index] = CompressFileAsync(inputFilePaths[index], outputDirectory, compressionAlgorithmOption, compressionLevelOption, cancellationToken);
            }
            await Task.WhenAll(tasks);
        }
        /// <inheritdoc/>
        public async Task DecompressFilesAsync(string[] inputFilePaths, string outputDirectory,CompressionAlgorithmOption compressionAlgorithmOption , CancellationToken cancellationToken = default)
        {
            if (inputFilePaths == null || inputFilePaths.Length == 0)
            {
                throw new ArgumentException("Input file paths cannot be null or empty.", nameof(inputFilePaths));
            }

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));
            }
            var tasks = new Task[inputFilePaths.Length];

            for (int i = 0; i < inputFilePaths.Length; i++)
            {
                int index = i;
                tasks[index] = DecompressFileAsync(inputFilePaths[index], outputDirectory,compressionAlgorithmOption, cancellationToken);
            }

            await Task.WhenAll(tasks);
        }
        private Stream GetCompressionStream(Stream outputStream, CompressionAlgorithmOption compressionAlgorithmOption, CompressionLevelOption compressionLevelOption)
        {
            switch (compressionAlgorithmOption)
            {
                case CompressionAlgorithmOption.GZip:
                    return new GZipStream(outputStream, (CompressionLevel)compressionLevelOption, false);
                case CompressionAlgorithmOption.Deflate:
                    return new DeflateStream(outputStream, (CompressionLevel)compressionLevelOption, false);
                case CompressionAlgorithmOption.Brotli:
                    return new BrotliStream(outputStream, (CompressionLevel)compressionLevelOption, false);
                default:
                    throw new InvalidOperationException("Unsupported compression algorithm.");
            }
        }
        private Stream GetDecompressionStream(Stream inputStream, CompressionAlgorithmOption compressionAlgorithmOption)
        {
            switch (compressionAlgorithmOption)
            {
                case CompressionAlgorithmOption.GZip:
                    return new GZipStream(inputStream, CompressionMode.Decompress);
                case CompressionAlgorithmOption.Deflate:
                    return new DeflateStream(inputStream, CompressionMode.Decompress);
                case CompressionAlgorithmOption.Brotli:
                    return new BrotliStream(inputStream, CompressionMode.Decompress);
                default:
                    throw new InvalidOperationException("Unsupported compression algorithm.");
            }
        }
        private async Task CompressFile(string inputFilePath, string outputFilePath, CompressionAlgorithmOption compressionAlgorithmOption, CompressionLevelOption compressionLevelOption, CancellationToken cancellationToken = default)
        {
            try
            {
                using (FileStream inputFileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                using (FileStream outputFileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                using (Stream compressionStream = GetCompressionStream(outputFileStream, compressionAlgorithmOption, compressionLevelOption))
                {
                    await inputFileStream.CopyToAsync(compressionStream, cancellationToken);
                }

                _logger.LogDebug($"File compressed successfully: {outputFilePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while compressing the file: {ex.Message}");
            }
        }
        private async Task DecompressFile(string inputFilePath, string outputFilePath, CompressionAlgorithmOption compressionAlgorithmOption, CancellationToken cancellationToken = default)
        {
            try
            {
                using (FileStream inputFileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                using (FileStream outputFileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                using (Stream decompressionStream = GetDecompressionStream(inputFileStream, compressionAlgorithmOption))
                {
                    await decompressionStream.CopyToAsync(outputFileStream, cancellationToken);
                }

                _logger.LogDebug($"File decompressed successfully: {outputFilePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while decompressing the file: {ex.Message}");
                throw;
            }
        }
    }
}
