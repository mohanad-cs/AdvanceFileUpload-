using System.Text;
using AdvanceFileUpload.Application.Compression;
using Microsoft.Extensions.Logging;
using Moq;

namespace AdvanceFileUpload.Domain.Test
{
    public class FileCompressorTests
    {
        private readonly Mock<ILogger<FileCompressor>> _loggerMock;
        private readonly FileCompressor _fileCompressor;

        public FileCompressorTests()
        {
            _loggerMock = new Mock<ILogger<FileCompressor>>();
            _fileCompressor = new FileCompressor(_loggerMock.Object);
        }

        [Fact]
        public async Task CompressFileAsync_ShouldCompressFile()
        {
            // Arrange
            string inputFilePath = Path.Combine(TestsUtility._tempDirectory, "test.txt");
            string outputDirectory = TestsUtility._tempDirectory;
            var compressionAlgorithm = CompressionAlgorithmOption.Brotli;
            var compressionLevel = CompressionLevelOption.Optimal;

            // Create a test file
            await File.WriteAllTextAsync(inputFilePath, TestsUtility.GenerateRandomText(1024 * 1024 * 2));

            // Act
            await _fileCompressor.CompressFileAsync(inputFilePath, outputDirectory, compressionAlgorithm, compressionLevel);

            // Assert
            string outputFilePath = Path.Combine(outputDirectory, "test.txt.gz");
            Assert.True(File.Exists(outputFilePath));

            //// Cleanup
            File.Delete(inputFilePath);
            File.Delete(outputFilePath);
        }

        [Fact]
        public async Task DecompressFileAsync_ShouldDecompressFile()
        {
            // Arrange
            string inputFilePath = Path.Combine(TestsUtility._tempDirectory, "test.txt");
            string outputDirectory = TestsUtility._tempDirectory;
            var compressionAlgorithm = CompressionAlgorithmOption.GZip;
            var compressionLevel = CompressionLevelOption.Optimal;

            // Create a test file and compress it
            await File.WriteAllTextAsync(inputFilePath, TestsUtility.GenerateRandomText(1024 * 1024 * 2));
            await _fileCompressor.CompressFileAsync(inputFilePath, outputDirectory, compressionAlgorithm, compressionLevel);

            string compressedFilePath = Path.Combine(outputDirectory, "test.txt.gz");
            File.Delete(inputFilePath);
            // Act
            await _fileCompressor.DecompressFileAsync(compressedFilePath, outputDirectory, compressionAlgorithm);

            // Assert
            string outputFilePath = Path.Combine(outputDirectory, "test.txt");
            Assert.True(File.Exists(outputFilePath));

            // Cleanup
            File.Delete(compressedFilePath);
            File.Delete(outputFilePath);
        }

        [Fact]
        public async Task CompressFilesAsync_ShouldCompressMultipleFiles()
        {
            // Arrange
            string[] inputFilePaths = { Path.Combine(TestsUtility._tempDirectory, "test1.txt"), Path.Combine(TestsUtility._tempDirectory, "test2.txt") };
            string outputDirectory = TestsUtility._tempDirectory;
            var compressionAlgorithm = CompressionAlgorithmOption.GZip;
            var compressionLevel = CompressionLevelOption.Optimal;

            // Create test files
            await File.WriteAllTextAsync(inputFilePaths[0], TestsUtility.GenerateRandomText(1024 * 1024 * 2));
            await File.WriteAllTextAsync(inputFilePaths[1], TestsUtility.GenerateRandomText(1024 * 1024 * 2));

            // Act
            await _fileCompressor.CompressFilesAsync(inputFilePaths, outputDirectory, compressionAlgorithm, compressionLevel);

            // Assert
            Assert.True(File.Exists(Path.Combine(outputDirectory, "test1.txt.gz")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "test2.txt.gz")));

            // Cleanup
            foreach (var filePath in inputFilePaths)
            {
                File.Delete(filePath);
                File.Delete(Path.Combine(outputDirectory, Path.GetFileName(filePath) + ".gz"));
            }
        }

        [Fact]
        public async Task DecompressFilesAsync_ShouldDecompressMultipleFiles()
        {
            // Arrange
            string[] inputFilePaths = { Path.Combine(TestsUtility._tempDirectory, "test1.txt"), Path.Combine(TestsUtility._tempDirectory, "test2.txt") };
            string outputDirectory = TestsUtility._tempDirectory;
            var compressionAlgorithm = CompressionAlgorithmOption.GZip;
            var compressionLevel = CompressionLevelOption.Optimal;

            // Create test files and compress them
            await File.WriteAllTextAsync(inputFilePaths[0], TestsUtility.GenerateRandomText(1024 * 1024 * 2));
            await File.WriteAllTextAsync(inputFilePaths[1], TestsUtility.GenerateRandomText(1024 * 1024 * 2));
            await _fileCompressor.CompressFilesAsync(inputFilePaths, outputDirectory, compressionAlgorithm, compressionLevel);

            string[] compressedFilePaths = {
                Path.Combine(outputDirectory, "test1.txt.gz"),
                Path.Combine(outputDirectory, "test2.txt.gz")
            };

            // Act
            await _fileCompressor.DecompressFilesAsync(compressedFilePaths, outputDirectory, compressionAlgorithm);

            // Assert
            Assert.True(File.Exists(Path.Combine(outputDirectory, "test1.txt")));
            Assert.True(File.Exists(Path.Combine(outputDirectory, "test2.txt")));

            // Cleanup
            foreach (var filePath in inputFilePaths)
            {
                File.Delete(filePath);
                File.Delete(Path.Combine(outputDirectory, Path.GetFileName(filePath) + ".gz"));
                File.Delete(Path.Combine(outputDirectory, Path.GetFileName(filePath)));
            }
        }
       
    }
}
