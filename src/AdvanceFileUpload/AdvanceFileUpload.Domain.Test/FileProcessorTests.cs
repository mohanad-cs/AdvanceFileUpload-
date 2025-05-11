using AdvanceFileUpload.Application.FileProcessing;
using Microsoft.Extensions.Logging.Abstractions;

namespace AdvanceFileUpload.Domain.Test
{
    public class FileProcessorTests
    {


        [Fact]
        public async Task SaveFileAsync_ShouldSaveFile()
        {
            // Arrange
            var fileProcessor = new FileProcessor(NullLogger<FileProcessor>.Instance);
            byte[] fileData = await File.ReadAllBytesAsync(TestsUtility._pdfTestFilePath);
            string filePath = Path.Combine(TestsUtility._tempDirectory, TestsUtility._fileName);

            // Act
            await fileProcessor.SaveFileAsync(TestsUtility._fileName, fileData, TestsUtility._tempDirectory);
            File.WriteAllBytesAsync(filePath, fileData);
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
            string filePath = TestsUtility._pdfTestFilePath;
            int expectedChunks = (int)Math.Ceiling((double)TestsUtility._fileSize / TestsUtility._maxChunkSize);
            // Act
            var chunkPaths = await fileProcessor.SplitFileIntoChunksAsync(filePath, TestsUtility._maxChunkSize, TestsUtility._tempDirectory);

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
            string outputFilePath = Path.Combine(TestsUtility._tempDirectory, "Merged.Pdf");
            string chunk1Path = Path.Combine(TestsUtility._tempDirectory, "chunk1.Pdf");
            string chunk2Path = Path.Combine(TestsUtility._tempDirectory, "chunk2.Pdf");
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
