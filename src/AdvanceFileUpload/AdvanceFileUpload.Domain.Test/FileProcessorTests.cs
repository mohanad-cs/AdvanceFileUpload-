using AdvanceFileUpload.Application.FileProcessing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AdvanceFileUpload.Domain.Test
{
    public class FileProcessorTests
    {
        private const string _testFilePath = @"..\TestFiles\testFile.Pdf";
        private const string _tempDirectory = @"..\Temp\";
        private string _fileName = Path.GetFileName(_testFilePath);
        private long _fileSize = new FileInfo(_testFilePath).Length;
        private long _maxChunkSize = 256 * 1024; // 256 KB

        [Fact]
        public async Task SaveFileAsync_ShouldSaveFile()
        {
            // Arrange
            var fileProcessor = new FileProcessor(NullLogger<FileProcessor>.Instance);
            byte[] fileData = await File.ReadAllBytesAsync(_testFilePath);
            string filePath = Path.Combine(_tempDirectory, _fileName);

            // Act
            await fileProcessor.SaveFileAsync(_fileName, fileData, _tempDirectory);

            // Assert
            Assert.True(File.Exists(filePath));
            var savedData = await File.ReadAllBytesAsync(filePath);
            Assert.Equal(fileData, savedData);

            // Cleanup
            File.Delete(filePath);
        }

        [Fact]
        public async Task SplitFileIntoChunksAsync_ShouldSplitFile()
        {
            // Arrange
            var fileProcessor = new FileProcessor(NullLogger<FileProcessor>.Instance);
            string filePath = _testFilePath;
            int expectedChunks = (int)Math.Ceiling((double)_fileSize / _maxChunkSize);
            // Act
            var chunkPaths = await fileProcessor.SplitFileIntoChunksAsync(filePath, _maxChunkSize, _tempDirectory);

            // Assert
            Assert.Equal(expectedChunks, chunkPaths.Count);
            foreach (var chunkPath in chunkPaths)
            {
                Assert.True(File.Exists(chunkPath));
            }

            // Cleanup
            foreach (var chunkPath in chunkPaths)
            {
                File.Delete(chunkPath);
            }
        }

        [Fact]
        public async Task MergeChunksAsync_ShouldMergeChunks()
        {
            // Arrange
            var fileProcessor = new FileProcessor(NullLogger<FileProcessor>.Instance);
            string outputFilePath = Path.Combine(_tempDirectory, "Merged.Pdf");
            string chunk1Path = Path.Combine(_tempDirectory, "chunk1.Pdf");
            string chunk2Path = Path.Combine(_tempDirectory, "chunk2.Pdf");
            await File.WriteAllBytesAsync(chunk1Path, new byte[] { 1, 2 });
            await File.WriteAllBytesAsync(chunk2Path, new byte[] { 3, 4 });
            var chunkPaths = new List<string> { chunk1Path, chunk2Path };

            // Act
            await fileProcessor.MergeChunksAsync(chunkPaths, outputFilePath);

            // Assert
            Assert.True(File.Exists(outputFilePath));
            var MergedData = await File.ReadAllBytesAsync(outputFilePath);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, MergedData);

            // Cleanup
            File.Delete(outputFilePath);
            File.Delete(chunk1Path);
            File.Delete(chunk2Path);
        }
    }

}
