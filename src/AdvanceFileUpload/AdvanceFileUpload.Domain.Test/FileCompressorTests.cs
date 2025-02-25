using AdvanceFileUpload.Application.Compression;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO.Compression;
using Xunit;

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
            string inputFilePath = "test.txt";
            string outputDirectory = "output";
            var compressionAlgorithm = CompressionAlgorithmOption.GZip;
            var compressionLevel = CompressionLevelOption.Optimal;

            // Create a test file
            await File.WriteAllTextAsync(inputFilePath, "Test content");

            // Act
            await _fileCompressor.CompressFileAsync(inputFilePath, outputDirectory, compressionAlgorithm, compressionLevel);

            // Assert
            string outputFilePath = Path.Combine(outputDirectory, "test.txt.gz");
            Assert.True(File.Exists(outputFilePath));

            // Cleanup
            File.Delete(inputFilePath);
            File.Delete(outputFilePath);
        }

        [Fact]
        public async Task DecompressFileAsync_ShouldDecompressFile()
        {
            // Arrange
            string inputFilePath = "test.txt";
            string outputDirectory = "output";
            var compressionAlgorithm = CompressionAlgorithmOption.GZip;
            var compressionLevel = CompressionLevelOption.Optimal;

            // Create a test file and compress it
            await File.WriteAllTextAsync(inputFilePath, "Test content");
            await _fileCompressor.CompressFileAsync(inputFilePath, outputDirectory, compressionAlgorithm, compressionLevel);

            string compressedFilePath = Path.Combine(outputDirectory, "test.txt.gz");

            // Act
            await _fileCompressor.DecompressFileAsync(compressedFilePath, outputDirectory, compressionAlgorithm);

            // Assert
            string outputFilePath = Path.Combine(outputDirectory, "test.txt");
            Assert.True(File.Exists(outputFilePath));

            // Cleanup
            File.Delete(inputFilePath);
            File.Delete(compressedFilePath);
            File.Delete(outputFilePath);
        }

        [Fact]
        public async Task CompressFilesAsync_ShouldCompressMultipleFiles()
        {
            // Arrange
            string[] inputFilePaths = { "test1.txt", "test2.txt" };
            string outputDirectory = "output";
            var compressionAlgorithm = CompressionAlgorithmOption.GZip;
            var compressionLevel = CompressionLevelOption.Optimal;

            // Create test files
            await File.WriteAllTextAsync(inputFilePaths[0], "Test content 1");
            await File.WriteAllTextAsync(inputFilePaths[1], "Test content 2");

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
            string[] inputFilePaths = { "test1.txt", "test2.txt" };
            string outputDirectory = "output";
            var compressionAlgorithm = CompressionAlgorithmOption.GZip;
            var compressionLevel = CompressionLevelOption.Optimal;

            // Create test files and compress them
            await File.WriteAllTextAsync(inputFilePaths[0], "Test content 1");
            await File.WriteAllTextAsync(inputFilePaths[1], "Test content 2");
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
