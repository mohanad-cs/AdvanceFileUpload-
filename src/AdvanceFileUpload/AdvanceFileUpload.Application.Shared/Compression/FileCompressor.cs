using System.Diagnostics;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace AdvanceFileUpload.Application.Compression
{
    /// <summary>
    /// Provides methods for compressing and decompressing files using various algorithms.
    /// </summary>
    public class FileCompressor : IFileCompressor
    {
        // a static list of applicable files extensions that can not be compressed because they are already compressed
        private static readonly List<string> _excludedCompressedExtensions = new List<string>
{
    // ===== Archive/Compressed Formats =====
    ".zip",      // ZIP Archive (standard compression)
    ".rar",      // RAR Archive (proprietary format)
    ".7z",       // 7-Zip Archive (high compression ratio)
    ".tar",      // Tape Archive (uncompressed bundle, often combined with compression)
    ".gz",       // Gzip Compressed File (common in Unix/Linux)
    ".bz2",      // Bzip2 Compressed File (better compression than gzip)
    ".xz",       // XZ Compressed File (LZMA2 algorithm)
    ".lz",       // Lzip Compressed File
    ".lzma",     // LZMA Compressed File
    ".cab",      // Microsoft Cabinet File (Windows installers)
    ".z",        // Unix Compress File (legacy format)
    ".tgz",      // TAR + Gzip Combined Archive
    ".tbz2",     // TAR + Bzip2 Combined Archive
    ".iso",      // Disk Image File (optical media format)
    ".dmg",      // Apple Disk Image (macOS installers)
    ".jar",      // Java Archive (executable Java package)
    ".war",      // Web Application Archive (Java web apps)
    ".ear",      // Enterprise Application Archive (Java EE)
    ".pak",      // Game Resource Archive (common in gaming)
    ".rpm",      // Red Hat Package Manager (Linux distros)
    ".deb",      // Debian Package (Debian/Ubuntu Linux)
    ".szip",     // SZIP Compressed Data (NASA format)
    ".lz4",      // LZ4 Compressed File (high-speed compression)
    ".msi",      // Windows Installer Package (uses CAB internally)
    ".apk",      // Android Application Package (ZIP-based)
    ".ipa",      // iOS Application Package (Apple ecosystem)
    ".lzh",      // LHA/LZH Archive (Japanese legacy format)

    // ===== Pre-Compressed Formats =====
    // Images
    ".jpg",      // JPEG Image (lossy compression)
    ".jpeg",     // JPEG Image (alternate extension)
    ".png",      // Portable Network Graphics (lossless compression)
    ".gif",      // Graphics Interchange Format (LZW compression)
    ".webp",     // WebP Image (Google's modern format)
    ".heic",     // HEIF Image (High Efficiency Image Format)
    ".heif",     // High Efficiency Image File Format
    ".tiff",     // Tagged Image File Format (often compressed)
    ".svgz",     // Compressed SVG (gzipped vector graphics)
    ".avif",     // AV1 Image File (next-gen royalty-free format)
    ".jp2",      // JPEG 2000 Image (wavelet compression)
    ".jxr",      // JPEG XR Image (HD Photo format)

    // Video
    ".mp4",      // MPEG-4 Video (H.264/265 compression)
    ".avi",      // Audio Video Interleave (container format)
    ".mkv",      // Matroska Video (open container format)
    ".mov",      // QuickTime Movie (Apple format)
    ".wmv",      // Windows Media Video (Microsoft format)
    ".flv",      // Flash Video (legacy web format)
    ".m4v",      // iTunes Video Format (MPEG-4 variant)
    ".mpg",      // MPEG Video (legacy compression)
    ".mpeg",     // MPEG Video (alternate extension)
    ".webm",     // WebM Video (VP8/VP9 compression)
    ".h264",     // H.264 Video Stream (AVC codec)
    ".h265",     // H.265 Video Stream (HEVC codec)
    ".hevc",     // High Efficiency Video Coding
    ".ogv",      // Ogg Video (open format)

    // Audio
    ".mp3",      // MPEG Audio Layer III
    ".aac",      // Advanced Audio Coding
    ".ogg",      // Ogg Vorbis Audio (open format)
    ".wav",      // WAVE Audio (sometimes compressed)
    ".flac",     // Free Lossless Audio Codec
    ".wma",      // Windows Media Audio
    ".m4a",      // MPEG-4 Audio (AAC codec)
    ".opus",     // Opus Audio (low-latency codec)
    ".ape",      // Monkey's Audio (lossless compression)
    ".alac",     // Apple Lossless Audio Codec

    // Documents
    ".pdf",      // Portable Document Format (often compressed)
    ".docx",     // Microsoft Word Document (ZIP-based)
    ".xlsx",     // Microsoft Excel Spreadsheet (ZIP-based)
    ".pptx",     // Microsoft PowerPoint Presentation (ZIP-based)
    ".odt",      // OpenDocument Text (ZIP-based)
    ".ods",      // OpenDocument Spreadsheet (ZIP-based)
    ".odp",      // OpenDocument Presentation (ZIP-based)
    ".xps",      // XML Paper Specification (Microsoft format)
    ".oxps",     // OpenXPS Document Format
    ".docm",     // Macro-Enabled Word Document
    ".xlsm",     // Macro-Enabled Excel Spreadsheet
    ".pptm",     // Macro-Enabled PowerPoint Presentation

    // Other
    ".epub",     // E-Book Format (ZIP-based)
    ".mobi",     // Mobipocket E-Book (Amazon Kindle)
    ".woff",     // Web Open Font Format
    ".woff2",    // Web Open Font Format v2 (brotli compression)
    ".br",       // Brotli Compressed Data (web optimization)
    ".zst",      // Zstandard Compressed Data
    ".cbz",      // Comic Book Archive (ZIP-based)
    ".cbr"       // Comic Book Archive (RAR-based)
};
        private readonly ILogger<FileCompressor> _logger;
        ///<inheritdoc/>
        public IReadOnlyList<string> ExcludedExtension => _excludedCompressedExtensions.AsReadOnly();
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
            await CompressFile(inputFilePath, outputFilePath, compressionAlgorithmOption, compressionLevelOption, cancellationToken);
        }
        /// <inheritdoc/>
        public async Task DecompressFileAsync(string inputFilePath, string outputDirectory, CompressionAlgorithmOption compressionAlgorithmOption, CancellationToken cancellationToken = default)
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
        public async Task DecompressFilesAsync(string[] inputFilePaths, string outputDirectory, CompressionAlgorithmOption compressionAlgorithmOption, CancellationToken cancellationToken = default)
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
                tasks[index] = DecompressFileAsync(inputFilePaths[index], outputDirectory, compressionAlgorithmOption, cancellationToken);
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
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                using (FileStream inputFileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                using (FileStream outputFileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                using (Stream compressionStream = GetCompressionStream(outputFileStream, compressionAlgorithmOption, compressionLevelOption))
                {
                    await inputFileStream.CopyToAsync(compressionStream, cancellationToken);
                }

                stopwatch.Stop();
                FileInfo inputFile = new FileInfo(inputFilePath);
                FileInfo outputFile = new FileInfo(outputFilePath);
                _logger.LogInformation($"File compressed successfully: {outputFilePath}. Original Size: {inputFile.Length} bytes, Compressed Size: {outputFile.Length} bytes, Time Taken: {stopwatch.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while compressing the file: {ex.Message}");
            }
        }
        private async Task DecompressFile(string inputFilePath, string outputFilePath, CompressionAlgorithmOption compressionAlgorithmOption, CancellationToken cancellationToken = default)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                using (FileStream inputFileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                using (FileStream outputFileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                using (Stream decompressionStream = GetDecompressionStream(inputFileStream, compressionAlgorithmOption))
                {
                    await decompressionStream.CopyToAsync(outputFileStream, cancellationToken);
                }

                stopwatch.Stop();
                FileInfo inputFile = new FileInfo(inputFilePath);
                FileInfo outputFile = new FileInfo(outputFilePath);
                _logger.LogInformation($"File decompressed successfully: {outputFilePath}. Compressed Size: {inputFile.Length} bytes, Decompressed Size: {outputFile.Length} bytes, Time Taken: {stopwatch.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while decompressing the file: {ex.Message}");
                throw;
            }
        }
        ///<inheritdoc/>
        public bool IsFileApplicableForCompression(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }
            string fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
            if (_excludedCompressedExtensions.Contains(fileExtension))
            {
                return false;
            }
            return true;
        }



        ///<inheritdoc/>
        public void AddExcludedExtension(string extension)
        {
            if (!string.IsNullOrWhiteSpace(extension))
            {
                extension = extension.Trim().ToLowerInvariant();
                if (!extension.StartsWith("."))
                {
                    extension = "." + extension;
                }
                if (_excludedCompressedExtensions.Contains(extension))
                {
                    _logger.LogWarning($"The extension '{extension}' is already in the excluded list.");
                    return;
                }
                _excludedCompressedExtensions.Add(extension);
            }

        }
    }
}
